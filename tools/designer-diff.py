#!/usr/bin/env python
"""筛查 WinForms Designer 文件的工作区变更，区分设计器噪音与真实改动。

用法:
    python tools/designer-diff.py            # 报告所有 Designer 文件的变更分类
    python tools/designer-diff.py --clean    # 逐行还原噪音，只保留真实改动

噪音判定：设计器打开窗体时按 AutoSize 实测结果重新序列化产生的派生数据，
包括 Size/Location 数值、语句顺序调整、空白差异、TabIndex 补默认值等。
这些值运行时会被重新计算覆盖，存进文件没有意义。

--clean 按行还原，因此含真实改动的文件也能自动清理，不必手工挑行；
判定为真实改动的行一律保留，仍需人工复核。
文件整体按字节读写，避免 BOM、CRLF 和中文注释触发本机编码问题。
"""
import re
import subprocess
import sys
from difflib import SequenceMatcher

# 视为噪音的行模式：仅数值或顺序变化，不改变布局语义
NOISE_PATTERNS = [
    re.compile(r'^\s*\w[\w.]*\.(Size|ClientSize)\s*=\s*new Size\('),
    re.compile(r'^\s*\w[\w.]*\.Location\s*=\s*new Point\('),
    re.compile(r'^\s*\w[\w.]*\.TabIndex\s*=\s*\d+;'),
    re.compile(r'^\s*\w[\w.]*\.(SetColumnSpan|SetRowSpan)\('),
    re.compile(r'^\s*\w[\w.]*\.(PerformLayout|ResumeLayout|SuspendLayout)\('),
    re.compile(r'^\s*\w+\s*=\s*new [\w.]+\(\);'),          # 控件声明/实例化顺序
    re.compile(r'^\s*//\s*$'),                              # 空注释行（尾随空格差异）
    re.compile(r'^\s*$'),
    # RowStyle/ColumnStyle 无参与显式 AutoSize 等价，属规范化写法
    re.compile(r'^\s*\w[\w.]*\.(Row|Column)Styles\.Add\(new (Row|Column)Style\((SizeType\.AutoSize)?\)\);'),
]


def is_noise(raw: bytes) -> bool:
    body = raw.decode('utf-8', 'replace')
    return any(p.match(body) for p in NOISE_PATTERNS)


def git_bytes(args: list[str]) -> bytes:
    return subprocess.run(['git'] + args, capture_output=True, check=True).stdout


def changed_designer_files() -> list[str]:
    out = git_bytes(['diff', '--name-only', '--', '*.Designer.cs']).decode('utf-8', 'replace')
    return [f for f in out.splitlines() if f.strip()]


def detect_eol(data: bytes) -> bytes:
    """取文件主导换行符。Designer 文件由 .gitattributes 固定为 CRLF。"""
    return b'\r\n' if data.count(b'\r\n') * 2 >= data.count(b'\n') else b'\n'


def read_versions(path: str) -> tuple[list[bytes], list[bytes], bytes]:
    """取 HEAD 与工作区两个版本的行，并给出工作区换行符。

    `git show` 和 `cat-file blob` 都不做检出转换，返回仓库内的 LF，而工作区被
    core.autocrlf 转成了 CRLF。因此这里统一按行剥离换行符再比较，只在写回时
    用工作区的换行符，避免整份文件被误判为差异或被写成 LF。
    """
    head_raw = git_bytes(['show', f'HEAD:{path}'])
    with open(path, 'rb') as handle:
        work_raw = handle.read()

    head = head_raw.replace(b'\r\n', b'\n').split(b'\n')
    work = work_raw.replace(b'\r\n', b'\n').split(b'\n')
    return head, work, detect_eol(work_raw)


def merge(head: list[bytes], work: list[bytes]) -> tuple[list[bytes], list[str], int]:
    """按行合并：噪音取 HEAD，真实改动取工作区。

    返回 (合并后的行, 真实改动的展示行, 已还原的噪音行数)。
    比较用去空白后的内容，因此纯空白差异会被一并还原。
    """
    matcher = SequenceMatcher(
        None,
        [line.strip() for line in head],
        [line.strip() for line in work],
        autojunk=False)

    merged: list[bytes] = []
    real: list[str] = []
    reverted = 0
    for tag, i1, i2, j1, j2 in matcher.get_opcodes():
        if tag == 'equal':
            # 内容一致时取 HEAD 原样，顺带还原尾随空格等纯空白噪音
            merged.extend(head[i1:i2])
            reverted += sum(1 for k in range(i2 - i1) if head[i1 + k] != work[j1 + k])
            continue

        removed, added = head[i1:i2], work[j1:j2]
        if all(is_noise(line) for line in removed + added):
            merged.extend(removed)
            reverted += max(len(removed), len(added))
            continue

        merged.extend(added)
        real.extend(f'-{line.decode("utf-8", "replace").rstrip()}' for line in removed)
        real.extend(f'+{line.decode("utf-8", "replace").rstrip()}' for line in added)

    return merged, real, reverted


def main() -> int:
    do_clean = '--clean' in sys.argv
    files = changed_designer_files()
    if not files:
        print('没有 Designer 文件发生变更。')
        return 0

    pure_noise: list[tuple[str, int]] = []
    has_real: list[tuple[str, list[str], int]] = []
    for path in files:
        head, work, eol = read_versions(path)
        merged, real, reverted = merge(head, work)
        if real:
            has_real.append((path, real, reverted))
        else:
            pure_noise.append((path, reverted))
        if do_clean:
            with open(path, 'wb') as handle:
                handle.write(eol.join(merged))

    if pure_noise:
        title = '=== 纯噪音（已还原） ===' if do_clean else '=== 纯噪音（可安全丢弃） ==='
        print(title)
        for path, reverted in pure_noise:
            print(f'  {path}  [{reverted} 行噪音]')

    if has_real:
        print('\n=== 含真实改动（需复核） ===')
        for path, real, reverted in has_real:
            state = f'{reverted} 行噪音已还原' if do_clean else f'{reverted} 行噪音'
            print(f'  {path}  [{len(real)} 行真实改动, {state}]')
            for line in real:
                print(f'      {line}')

    if do_clean:
        print('\n噪音已按行还原，上述真实改动保留在工作区，请复核后再暂存。')
    else:
        print('\n加 --clean 参数可按行还原噪音，只留真实改动。')

    return 0


if __name__ == '__main__':
    raise SystemExit(main())

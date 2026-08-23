#!/usr/bin/env python
"""筛查 WinForms Designer 文件的工作区变更，区分设计器噪音与真实改动。

用法:
    python tools/designer-diff.py            # 报告所有 Designer 文件的变更分类
    python tools/designer-diff.py --clean    # 丢弃纯噪音文件的变更，保留含真实改动的文件

噪音判定：设计器打开窗体时按 AutoSize 实测结果重新序列化产生的派生数据，
包括 Size/Location 数值、语句顺序调整、空白差异、TabIndex 补默认值等。
这些值运行时会被重新计算覆盖，存进文件没有意义。
"""
import re
import subprocess
import sys

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


def is_noise(line: str) -> bool:
    body = line[1:]
    return any(p.match(body) for p in NOISE_PATTERNS)


def changed_designer_files() -> list[str]:
    out = subprocess.run(
        ['git', 'diff', '--name-only', '--', '*.Designer.cs'],
        capture_output=True, text=True, check=True).stdout
    return [f for f in out.splitlines() if f.strip()]


def classify(path: str) -> tuple[list[str], list[str]]:
    """返回 (真实改动行, 噪音行)。比较时忽略空白差异。"""
    diff = subprocess.run(
        ['git', 'diff', '-w', '--', path],
        capture_output=True, text=True, check=True).stdout

    real, noise = [], []
    for line in diff.splitlines():
        if not line or line[0] not in '+-':
            continue
        if line.startswith(('+++', '---')):
            continue
        (noise if is_noise(line) else real).append(line)
    return real, noise


def main() -> int:
    do_clean = '--clean' in sys.argv
    files = changed_designer_files()
    if not files:
        print('没有 Designer 文件发生变更。')
        return 0

    pure_noise, has_real = [], []
    for path in files:
        real, noise = classify(path)
        if real:
            has_real.append((path, real, noise))
        else:
            pure_noise.append((path, noise))

    if pure_noise:
        print('=== 纯噪音（可安全丢弃） ===')
        for path, noise in pure_noise:
            print(f'  {path}  [{len(noise)} 行噪音]')

    if has_real:
        print('\n=== 含真实改动（需保留并复核） ===')
        for path, real, noise in has_real:
            print(f'  {path}  [{len(real)} 行真实改动, {len(noise)} 行噪音]')
            for line in real:
                print(f'      {line.rstrip()}')

    if do_clean:
        if pure_noise:
            paths = [p for p, _ in pure_noise]
            subprocess.run(['git', 'restore', '--'] + paths, check=True)
            print(f'\n已丢弃 {len(paths)} 个纯噪音文件的变更。')
        else:
            print('\n没有可丢弃的纯噪音文件。')
        if has_real:
            print('以下文件含真实改动，未做处理，请自行复核后提交：')
            for path, _, _ in has_real:
                print(f'  {path}')
    elif pure_noise:
        print('\n加 --clean 参数可丢弃上述纯噪音变更。')

    return 0


if __name__ == '__main__':
    raise SystemExit(main())

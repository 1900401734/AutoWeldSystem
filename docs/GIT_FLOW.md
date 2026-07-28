# Git Flow 操作手册

本仓库采用轻量 Git Flow：两条长期分支 + 三类短期分支。个人开发,按"谁写的代码"选择快速通道或 PR 通道。

## 模型总览

```text
main ─────●────────────────●────────→  发布分支:只接受 develop/hotfix 合并,打 tag,不直接开发
           \              /
develop ────●──●──●──●──●──●──●───→  集成分支:所有改动的目的地,主目录常驻此分支
              \__/\__/
            feature/fix 短期分支,寿命以天计
```

- `main`:现场部署基准。只在 develop 实测稳定后合入并打 tag。
- `develop`:日常集成分支。所有短期分支从这里切、合并回这里。

## 通道一:快速通道(自己写的小改动)

适用:改文案、调布局、小修复等自己完全掌握的改动。不推远程功能分支、不提 PR。

```bash
git checkout develop && git pull
git checkout -b fix/log-wording
# ...改代码,提交...
git checkout develop
git merge --no-ff fix/log-wording -m "fix(log): 调整日志措辞"
git push
git branch -d fix/log-wording
```

要点:

- 必须 `--no-ff`:保留合并节点,历史上能看清"这是一次完整改动"。
- 合并信息写清改了什么、怎么验证的——这就是项目的变更记录。

## 通道二:PR 通道(AI 产出或跨模块大改动)

适用:Claude/Codex 等 AI 写的代码、跨多个模块的改动、影响现场行为的变更。PR 页面是合并前集中审核 AI 产出的闸口。

```bash
git checkout develop && git pull
git checkout -b feature/report-export-excel
# ...开发,小步提交...
git push -u origin feature/report-export-excel
gh pr create --base develop
# 审核 → 合并(rebase-merge 或 --no-ff)→ 删除远程分支
git checkout develop && git pull
git branch -d feature/report-export-excel
```

折中方案:AI 在本地合并前先给出 `git diff` 摘要,人工点头后就地合并——审核不省,省掉 PR 往返。

## 发版循环

```bash
git checkout main
git merge --no-ff develop -m "release: v1.1.0"
git tag v1.1.0
git push origin main --tags
git checkout develop
```

## 紧急修复(现场故障,develop 上有未验证功能不能发)

```bash
git checkout -b hotfix/telemetry-crash main   # 从 main 切,不是 develop!
# ...修复、提交、实测...
git checkout main && git merge --no-ff hotfix/telemetry-crash
git tag v1.1.1 && git push origin main --tags
git checkout develop && git merge main        # 回合 develop,两边都含修复
git push && git branch -d hotfix/telemetry-crash
```

## 防乱三条纪律

1. **分支寿命 < 一周**:合并即删,本地远程都删。分支列表永远只有 main/develop + 手头 1-2 个活跃分支。
2. **一个分支只做一件事**:不在一个分支上攒多个功能。
3. **不做 backup 分支**:要保险就打 tag(`git tag backup/before-big-change && git push --tags`),30 天内还可用 `git reflog` 找回任何提交。

## 踩过的坑:rebase-merge 的"合并前镜像"

用 rebase 方式合并 PR 后,功能分支上的旧提交仍在(SHA 不同、内容相同),看起来"未合并"。判断分支是否真有未合并内容:

```bash
git cherry develop <branch>     # 以 + 开头的才是 develop 没有的补丁
```

对已合并分支追加改动:`git rebase --onto origin/develop <旧基点>` 到新分支再开 PR,不在已合并旧分支上续推。

## AI 协作约定

- 让 AI 开工时说清"基于 develop 做 xxx",由 AI 自己切分支、选通道。
- AI 大改动走 PR 通道或合并前给 diff 摘要;文档/小修复可走快速通道。
- 合并后让 AI"清理分支":先 `git cherry` 验证零损失再删。

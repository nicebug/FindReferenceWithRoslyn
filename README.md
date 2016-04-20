# FindReferenceWithRoslyn

## 工作原理
1. svn diff -r xxx:yyy 生成两个版本的差异文件，svndiff.txt
2. methodinfo.txt: 生成工程中函数的起始行
3. 关联svndiff.txt和methodinfo.txt查出变更的函数名和文件名
4. 跟进3的结果查询变更函数的相关引用
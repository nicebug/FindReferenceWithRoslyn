# FindReferenceWithRoslyn
## 工作原理
1. svn diff -r xxx:yyy 生成两个版本的差异文件，svndiff.txt
2. command line根据sln生成函数信息
3. 关联svndiff.txt和步骤2产生的函数信息查出变更的函数名和文件名
4. 根据步骤3的结果查询变更函数的相关引用
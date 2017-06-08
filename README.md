# 12306火车票查询工具

## 安装依赖
dotnet restore

## 运行

参数顺序：起点站 到达站 时间 列车类型（G,D,Z,T,K），多个用（英文半角逗号）隔开

### 常规查询

dotnet run 北京 天津 2017-06-08 G,D,Z,T

### 精确车站查询

只需要在车站名称前加上^符号即可

dotnet run ^北京 天津 2017-06-09 G

## 发布
dotnet publish -c Release -r win10-x64

## 截图
![](./screenshots/query.png)

## TODO

- 参数指定排序方式（出发到达时间，历时）

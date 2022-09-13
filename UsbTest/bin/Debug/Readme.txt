使用说明

1、Open 打开串口

2、GetFile 选择要下发的文件
     下发格式: 55 AA len(2byte) data...      一次5K
      data=1024 * 5;//一包数据大小

3、同一级目录会接收到 单片机发回的数据 Temp.TXT 文件中，用对比工具BCompare 可以知道是否丢帧
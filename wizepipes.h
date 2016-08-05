#ifndef __wizepipes
#define __wizepipes

#include "msp430x24x.h"
#include "stdlib.h"
#include "string.h"

#define CPU_CLK 16000000 //CPU工作时钟为16M

#define C_OK            1
#define C_ERROR         2
#define C_FAIL          3
#define C_BUILDED       4
#define D_IPD           5
#define D_INPUT         6
#define C_UNLINK        7
#define C_RCOMMAND      8
#define C_WRONG         9
#define C_READY         10

#define MAX_BUFFER 96


//void _itoa(char num, char *buf, char radix);

typedef struct wifidata
{
  char IsWifidefault;//是否默认wifi账号密码登陆
  char IsWifiConnected;
  char Baute;
  char Type;//  通讯协议
  char ServerPort[8];
  char Ssid[15];
  char Password[12];
  char ServerIP[20];
 // char ID[5];
} WDATA;

typedef struct configdata
{
  short hour;
  short minute;
  char ID[5];
} CDATA;

typedef struct capturedata
{
  char hour;
  char minute;
  char seconds;
  char ms;
} TDATA;


void delaymsec();
void delay();
void InitialSystem();
void ReadConfig();
unsigned long ReadGPSInfo();
char Connectwifi();  
void StartTimer();
void StopTimer();
void dotask();
void docapture();
void sendheartbeat();
unsigned char senddata(char sbuf[],unsigned char slen);
void MX29LV320t_Cmd_Reset(void);
void MX29LV320t_Cmd_Erase_Chip();  
void MX29LV320t_Flash_Write(unsigned long vaddress, unsigned char vdata);
unsigned char MX29LV320t_Flash_Read(unsigned long vaddress);
void MX29LV320t_Command_Write(unsigned long vaddress, unsigned char vdata);
void write_SegB();
void OutTimer();
void Gps_MSG();
unsigned char Gps_senddata(char sbuf[],unsigned char slen);

#endif /* #ifndef __wizewizepipes */
#include "wizepipes.h"

extern WDATA wd1;
extern CDATA cd1;
extern TDATA td1;
extern char raddr,IsHaveCommand,IsReceiveCommand;
extern  char ATMSG[];
extern char netstatus;
extern char rbuffer[];
extern char gpsrbuf[MAX_BUFFER];
extern char gpstbuf[MAX_BUFFER];
extern char isgpscmdok;
extern char gpsaddr;
extern char capOK;
extern char  IsCaptureTime;
extern unsigned int Timing;
extern unsigned long flash_addr;
extern unsigned long sum;
extern unsigned short counter_tx; 
extern  char ADdata[MAX_BUFFER];
extern  char temp[MAX_BUFFER];

void delaymsec()
{
 unsigned long i;
 for(i=0x4FFFFF;i>0;i--)_NOP();
}

void InitialSystem()
{
 // unsigned long i;

  // Stop watchdog timer to prevent time out reset
  WDTCTL = WDTPW + WDTHOLD; 
 
  //改为使用XT2
  BCSCTL1 &= ~XT2OFF;                       // Activate XT2 high freq xtal
  BCSCTL1 &= ~XTS;                          //LFXT1低频率模式(32.768K)
  BCSCTL1 |=DIVA_3;                          //ACLK=32.768k/8=4096
  BCSCTL3 |= XT2S_2;                        // 使用3-16M外部晶体
  BCSCTL3 |= XCAP_2;                        // XIN/XOUT Cap : 10 pF 
  BCSCTL3 |= LFXT1S_0;
  
  do
  {
    IFG1 &= ~OFIFG;                         // Clear OSCFault flag
    for (int i = 0xFF; i > 0; i--);             // Time for flag to set
  }
  while (IFG1 & OFIFG);                     // OSCFault flag still set?

  BCSCTL2 |= SELM_2;                        // MCLK = XT2 HF XTAL (safe)
  BCSCTL2 |= SELS;                          
  BCSCTL2 |= DIVS_1;                        // SCLK = XT2/2  
  //修改时钟结束
  
  FCTL2 = FWKEY + FSSEL1 + FN1;             // MCLK/3 for Flash Timing Generator
  
  //初始化端口
  P1DIR = 0xFF;                             // Flash 8位数据线
 // P1OUT |= 0xFF;                             
  
  P2DIR |= 0xBF;                             // A16-21 A-1 P2.6 1PPS
//  P2OUT |= 0xBF;                                // All P2.x reset
  
  P3SEL = 0xF0;                            // P3.3,2 USCI_B0 option select
                                            // P3.4,5 = USCI_A0 TXD/RXD
                                            //P3.6,7 = USCI_A1 TXD/RXD
 //P3.0 = CFG_GPS0 output P3.1 = extint0_GPS input P3.2 NC  output P3.3 WIFI_CH_PD output
  P3DIR |=0x0D;
  P3DIR &= ~0x02; //P3.2 = DOUT input
  P3OUT |= 0x0D; 

  
  P4DIR = 0xFF;                             // A0-A7
 // P4OUT = 0x00;                                // All P4.x reset
  
  P5DIR = 0xFF;                             // A8-A15
 // P5OUT = 0x00;                                // All P5.x reset

  
  P6SEL = 0x03;                             // P3.0,1 = AD input
  P6DIR |= 0xFC;                             // P6.0~1 inputs,P6.2~7 outputs
  
  //UART0 config to wifi
 /* UCA0CTL1 |= UCSSEL_2;                     // CLK = SMCLK
  UCA0BR0 = 0x41;                           // 8MHz/115200 = 69
  UCA0BR1 = 0x03;                           //
  UCA0MCTL = UCBRS_2 + UCBRF_0;      // Modulation UCBRSx = 4 UCBRFx=0  
  UCA0CTL1 &= ~UCSWRST;                     // **Initialize USCI state machine**
  IE2 &= ~UCA0RXIE; */

  //UART0 config to wifi
  UCA0CTL1 |= UCSSEL_2;                     // CLK = SMCLK
  UCA0BR0 = 69;                           // 8MHz/115200 = 69
  UCA0BR1 = 0x00;                           //
  UCA0MCTL = UCBRS_4 + UCBRF_0;      // Modulation UCBRSx = 4 UCBRFx=0  
  UCA0CTL1 &= ~UCSWRST;                     // **Initialize USCI state machine**
  IE2 &= ~UCA0RXIE;

  
  //UART1 config to GPS
  UCA1CTL1 |= UCSSEL_2;                     // CLK = SMCLK
  UCA1BR0 = 0xD0;                              // 8MHz/38400 = 208
  UCA1BR1 = 0x00;                              // 
  UCA1MCTL = UCBRS_3 + UCBRF_0;             // Modulation UCBRSx = 0
  UCA1CTL1 &= ~UCSWRST;                     // **Initialize USCI state machine**   
  UC1IE &= ~UCA1RXIE;
  
  //reset external flash
  MX29LV320t_Cmd_Reset();
}

void ReadConfig()
{
  char i;
  
  char *addr=(char *)0x1040;  
  wd1.IsWifidefault=*addr++;
  wd1.IsWifiConnected=*addr++;
  wd1.Baute=*addr++;
  wd1.Type=*addr++;
  for(i=0;i<8;i++)wd1.ServerPort[i]=*addr++;
  for(i=0;i<15;i++)wd1.Ssid[i]=*addr++;
  for(i=0;i<12;i++)wd1.Password[i]=*addr++; 
  for(i=0;i<20;i++)wd1.ServerIP[i]=*addr++;
  
  addr=(char *)0x1000;  
  cd1.hour=*(short *)addr++;
  addr++;
  cd1.minute=*(short *)addr++;
  for(i=0;i<5;i++)cd1.ID[i]=*addr++;
}


void write_SegB()
{
  char *Flash_ptr;                          // Flash pointer
  unsigned int i;

  Flash_ptr = (char *)0x1080;               // Initialize Flash pointer
  FCTL3 = FWKEY;                            // Clear Lock bit
  FCTL1 = FWKEY + ERASE;                    // Set Erase bit
  *Flash_ptr = 0;                           // Dummy write to erase Flash seg

  FCTL1 = FWKEY + WRT;                      // Set WRT bit for write operation

  for (i = 9; i<13; i++)
  {
    *Flash_ptr++ = temp[i];                   // Write value to flash
  }
  
  FCTL1 = FWKEY;                            // Clear WRT bit
  FCTL3 = FWKEY + LOCK;                     // Set LOCK bit
  
  Flash_ptr = (char *)0x1080;               // Initialize Flash pointer
  
  td1.hour = *Flash_ptr++;                  //将采样时间写入结构体中
  td1.minute = *Flash_ptr++;
  td1.seconds = *Flash_ptr++;
  td1.ms = *Flash_ptr++;
}

unsigned long ReadGPSInfo()
{      
      char i;
      unsigned long tmp1=0;

      UC1IE |= UCA1RXIE;
      while(isgpscmdok!=2)_NOP();
      UC1IE &= ~UCA1RXIE;
      
      //提取时间  
      for(i=7;i<17;i++)gpsrbuf[i] -=0x30;
      //计算小时
      tmp1=0;
      tmp1 = gpsrbuf[7]*10 + gpsrbuf[8] + 8;     
      if((tmp1>=24)&&(tmp1<32))tmp1 -= 24;
      if(tmp1>=32) tmp1 = 0;
      //计算分钟	         
      tmp1 =(unsigned long)(tmp1*60);
      tmp1 = (unsigned long)(gpsrbuf[9]*10 + gpsrbuf[10]+tmp1);
      //计算秒
      tmp1 =(unsigned long)(tmp1*60);
      tmp1 = (unsigned long)(gpsrbuf[11]*10 + gpsrbuf[12]+tmp1);
      return(tmp1);
}

void ReturnGPSInfo()
{
  char i;
  
  UC1IE |= UCA1RXIE;
  while(isgpscmdok!=2)_NOP();
  UC1IE &= ~UCA1RXIE;
  
  IsHaveCommand=0;
  raddr=0; 
  strcpy(ATMSG,"AT+CIPSEND=6\r\n");//发送连接指示
  senddata(ATMSG,strlen(ATMSG));
  while(IsHaveCommand!=D_INPUT) _NOP();//等待响应指令 
  _NOP();
  for(i=7;i<13;i++)
  {
  ATMSG[i-7]=gpsrbuf[i];
  }
  i=(ATMSG[0]-0x30)*10+ATMSG[1]-0x30+8;
  ATMSG[0]=i/10+0x30;
  ATMSG[1]=i%10+0x30;
  senddata(ATMSG,6);    
  while(IsHaveCommand!=C_OK) _NOP();//等待响应指令     
}


unsigned char senddata(char sbuf[],unsigned char slen)
{
  for(counter_tx=0;counter_tx<slen;counter_tx++)
  {
    while (!(IFG2&UCA0TXIFG));  
    UCA0TXBUF=sbuf[counter_tx];   
  }
   return 0;
}

char Connectwifi() 
{  
  IsHaveCommand =0;
  raddr=0; 
  strcpy(ATMSG,"ATE0\r\n");//啰嗦开关ATE0关闭ATE1开启
  senddata(ATMSG,strlen(ATMSG));   
  while(IsHaveCommand!=C_OK) _NOP();//等待响应指令 
  
  IsHaveCommand =0;
  raddr=0; 
  strcpy(ATMSG,"AT+CWMODE?\r\n");//设置模式
  senddata(ATMSG,strlen(ATMSG));   
  while(IsHaveCommand!=C_OK) _NOP();//等待响应指令
  
  if(rbuffer[8]!='1')//1 station 2 AP 3 station &AP
  {  
    IsHaveCommand =0;
    raddr=0; 
    strcpy(ATMSG,"AT+CWMODE=1\r\n");//设置模式
    senddata(ATMSG,strlen(ATMSG));   
    while(IsHaveCommand!=C_OK) _NOP();//等待响应指令
  } 
  
  IsHaveCommand =0;
  raddr=0; 
  strcpy(ATMSG,"AT+CWJAP?\r\n");//设置模式
  senddata(ATMSG,strlen(ATMSG));   
  while(IsHaveCommand!=C_OK) _NOP();//等待响应指令 
    
  //delaymsec();//等待信息输出完成
  
  if(rbuffer[0]=='N' && rbuffer[1]=='o' && rbuffer[2]==0x20 && rbuffer[3]=='A' && rbuffer[4]=='P')
  {  
    IsHaveCommand =0;
    raddr=0;
    if(wd1.Ssid[0]!=0xFF)
    {
      strcpy(ATMSG,"AT+CWJAP=\"");//连接路由器
      strcat(ATMSG,wd1.Ssid);
      strcat(ATMSG,"\",\"");
      if(wd1.Password[0]!=0xFF)strcat(ATMSG,wd1.Password); 
      else strcat(ATMSG,"0000"); 
      strcat(ATMSG,"\"\r\n");    
      senddata(ATMSG,strlen(ATMSG));
      while(!IsHaveCommand) _NOP();//等待响应指令
      //delaysec(WAITRECALL);
      //if(IsHaveCommand==C_FAIL ){IsHaveCommand =0;return 0;}
    } 
    if(IsHaveCommand==C_FAIL || IsHaveCommand ==0 )
    {
      strcpy(ATMSG,"AT+CWJAP=\"TP-LINK_0924\",\"dong8-304\"\r\n");//连接路由器 
      //strcpy(ATMSG,"AT+CWJAP=\"Wizepipes\",\"VP2A4007\"\r\n");//连接路由器 
      //strcpy(ATMSG,"AT+CWJAP=\"TP-LINK\",\"windwood\"\r\n");//连接路由器      
      senddata(ATMSG,strlen(ATMSG));
      while(!IsHaveCommand) _NOP();//等待响应指令  
      if(IsHaveCommand==C_FAIL ){IsHaveCommand =0;return 0;}      
    }  
  }
  
  IsHaveCommand=0;
  raddr=0;
  strcpy(ATMSG,"AT+CIPSTATUS\r\n");//查询IP状态
  senddata(ATMSG,strlen(ATMSG));
  while(!IsHaveCommand) _NOP();//等待响应指令 
  //delaymsec();
  
  if(rbuffer[7]!='3')//2 get ip 3 builded 4 lost ip
  {
    IsHaveCommand =0;
    raddr=0;
    if(wd1.ServerIP[0]!=0xFF)
    {
      strcpy(ATMSG,"AT+CIPSTART=\"");//建立IP链接 
      switch(wd1.Type)
      {
        case 0: strcat(ATMSG,"TCP\",\"");break;
        case 1: strcat(ATMSG,"UDP\",\"");break;
        default:
                strcat(ATMSG,"TCP\",\"");break;  
      }
      strcat(ATMSG,wd1.ServerIP);
      strcat(ATMSG,"\",");
      if(wd1.ServerPort[0]!=0xFF)strcat(ATMSG,wd1.ServerPort);
      else strcat(ATMSG,"0000");
      strcat(ATMSG,"\r\n");
      senddata(ATMSG,strlen(ATMSG));
      while(!IsHaveCommand) _NOP();//等待响应指令
      delaymsec();
     // if(IsHaveCommand==C_ERROR){IsHaveCommand =0; return 0;}
    }
    if(IsHaveCommand==C_ERROR || IsHaveCommand ==0)
    {
      IsHaveCommand=0;
      raddr=0;
      //strcpy(ATMSG,"AT+CIPSTART=\"TCP\",\"120.25.229.254\",8080\r\n");//建立IP链接 
      // strcpy(ATMSG,"AT+CIPSTART=\"TCP\",\"192.168.1.127\",8080\r\n");//建立IP链接 
      strcpy(ATMSG,"AT+CIPSTART=\"TCP\",\"192.168.1.100\",8080\r\n");//建立IP链接 
     // strcpy(ATMSG,"AT+CIPSTART=\"TCP\",\"192.168.2.101\",8080\r\n");//建立IP链接 
      senddata(ATMSG,strlen(ATMSG));
      while(!IsHaveCommand) _NOP();//等待响应指令 
      delaymsec();
      if(IsHaveCommand==C_ERROR){IsHaveCommand =0;return 0;}    
    } 
  }
  netstatus = 1;
  IsHaveCommand = 0;
  return 1;
}

void StartTimer()
{
 //定时时间间隔为15秒  
  CCTL0 = CCIE;                             // CCR0 interrupt enabled
  CCR0 = 0x2FFF;
  TACTL = TASSEL_1 + MC_1 + ID_3;                  // ACLK/8=4096Hz, upmode  
}

void StopTimer()
{
  //暂停定时器
  //关闭时间定时器Timer A0
  CCTL0 &= ~CCIE;                  // CCR0 interrupt disabled
  //关闭计数
  TACTL = MC_0; 
}

void sendheartbeat()
{
  IsHaveCommand=0;
  raddr=0; 
  strcpy(ATMSG,"AT+CIPSEND=12\r\n");//发送连接指示
  senddata(ATMSG,strlen(ATMSG));
  while(IsHaveCommand!=D_INPUT) _NOP();//等待响应指令 
  _NOP();
 // delaymsec();//等待信息输出完成

    ATMSG[0]=0xA5;
    ATMSG[1]=0xA5;    
    ATMSG[2]=0xFF;
    ATMSG[3]=0x00;
    ATMSG[4]=0x04;
    ATMSG[5]=cd1.ID[0];
    ATMSG[6]=cd1.ID[1];
    ATMSG[7]=cd1.ID[2];
    ATMSG[8]=cd1.ID[3];
    ATMSG[9]=0xFF;  
    ATMSG[10]=0x5A;
    ATMSG[11]=0x5A;        
    senddata(ATMSG,12);    
    while(IsHaveCommand!=C_OK) _NOP();//等待响应指令     
}

void dotask()
{
  // char temp[MAX_BUFFER];
   unsigned int k=0;
   unsigned char j=0;
   IsReceiveCommand=0;
   
   //for(char i=0;i<raddr;i++) temp[i]=rbuffer[i];    
   for(char i=0;rbuffer[i-1]!=0x5A;i++)
     temp[i]=rbuffer[i];    
   switch(temp[2]) //命令参数     
   {
   case 0x21://DO操作
      
      break;
      
   case 0x22:
      ADC12CTL0 = SHT0_2 + ADC12ON;             // Set sampling time, turn on ADC12
      ADC12CTL0 |= REF2_5V;
      ADC12CTL1 = SHP;                          // Use sampling timer
      ADC12MCTL0 = SREF_2+INCH_1;
      IsCaptureTime=1;      
      break;
      
      case 0x23:
       flash_addr=(temp[9]<<12) +(temp[10]<<8) + temp[11];
       if( flash_addr%2==0)
       {
         if(((temp[12]<<8)+temp[13])>2048 );
         else
         {
         k=(temp[12]<<8)+temp[13];
       /*IsHaveCommand=0;
       raddr=0; 
       strcpy(ATMSG,"AT+CIPSEND\r\n");//开启透传模式
       senddata(ATMSG,strlen(ATMSG));
       while(!IsHaveCommand) _NOP();//等待响应指令 
       */ 
         while(k>0)
         {
           ATMSG[j]=k%10;
           k=k/10;
           j++;        
         }
        ATMSG[j]='\0';
        while(j>0)
        {
          j--;         
          ADdata[k]=ATMSG[j]+48;
          k++;
        }
        ADdata[k]='\r';
        ADdata[k+1]='\n';
        ADdata[k+2]='\0';
        k=(temp[12]<<8)+temp[13];
         IsHaveCommand=0;
         raddr=0;
         strcpy(ATMSG,"AT+CIPSEND=");//发送连接指示 
         strcat(ATMSG, ADdata);
         senddata(ATMSG,strlen(ATMSG)); 
         while(IsHaveCommand!=D_INPUT) _NOP();//等待响应指令 
         _NOP();
            
            for(int i=0;i<k;i++)
            {
              while (!(IFG2&UCA0TXIFG));  
              UCA0TXBUF=MX29LV320t_Flash_Read(flash_addr);
              flash_addr++;
            } 
         while(IsHaveCommand!=C_OK) _NOP();//等待响应指令
         }
       }     
       StartTimer();           
      break;
      
      case 0x24:
        ReturnGPSInfo();
        StartTimer();
       break;
       
   case 0x25:
     if(temp[7]==0x0)
     {
       IsHaveCommand=0;
       raddr=0; 
       strcpy(ATMSG,"AT+CIPSEND=16\r\n");//发送连接指示
       senddata(ATMSG,strlen(ATMSG));
       while(IsHaveCommand!=D_INPUT) _NOP();//等待响应指令 
      _NOP();

      ATMSG[0]=0xA5;
      ATMSG[1]=0xA5;    
      ATMSG[2]=0x25;    
      ATMSG[3]=cd1.ID[0];     
      ATMSG[4]=cd1.ID[1]; 
      ATMSG[5]=cd1.ID[2];     
      ATMSG[6]=cd1.ID[3];     
      ATMSG[7]=0x00;     
      ATMSG[8]=0x04;     
      ATMSG[9]=td1.hour;     
      ATMSG[10]=td1.minute;     
      ATMSG[11]=td1.seconds;     
      ATMSG[12]=td1.ms;
      ATMSG[13]=0xFF;
      ATMSG[14]=0xA5;
      ATMSG[15]=0xA5;
      senddata(ATMSG,16);    
      while(IsHaveCommand!=C_OK) _NOP();//等待响应指令    
     }
     else
     write_SegB();
     StartTimer();
     break;
     
   case 0x26:
     Timing = (temp[9]<<8)+temp[10];
     StartTimer();
       break;
      
   default: 
          
     raddr=0;
     IsHaveCommand=0;
     IsReceiveCommand=0; 
     strcpy(ATMSG,"AT+CIPSEND=13\r\n");//发送连接指示     
     senddata(ATMSG,strlen(ATMSG)); 
     while(IsHaveCommand!=D_INPUT) _NOP();//等待响应指令
     _NOP();
     ATMSG[0]=0xA5;     
     ATMSG[1]=0xA5;     
     ATMSG[2]=0xFE;     
     ATMSG[3]=cd1.ID[0];     
     ATMSG[4]=cd1.ID[1]; 
     ATMSG[5]=cd1.ID[2];     
     ATMSG[6]=cd1.ID[3];     
     ATMSG[7]=0x00;     
     ATMSG[8]=0x01;     
     ATMSG[9]=0xFF;     
     ATMSG[10]=0xFF;     
     ATMSG[11]=0x5A;     
     ATMSG[12]=0x5A;     
     senddata(ATMSG,13);
     while(IsHaveCommand!=C_OK) _NOP();//等待响应指令 
     StartTimer();
  }
}

void docapture()
{  
   IsHaveCommand=0;
   raddr=0; 
   strcpy(ATMSG,"AT+CIPSEND=13\r\n");//发送连接指示
   senddata(ATMSG,strlen(ATMSG));
   while(IsHaveCommand!=D_INPUT) _NOP();//等待响应指令 
   _NOP();
   ATMSG[0]=0xA5;     
   ATMSG[1]=0xA5;     
   ATMSG[2]=0xFD;     
   ATMSG[3]=cd1.ID[0];     
   ATMSG[4]=cd1.ID[1]; 
   ATMSG[5]=cd1.ID[2];     
   ATMSG[6]=cd1.ID[3];     
   ATMSG[7]=0x00;     
   ATMSG[8]=0x01;     
   ATMSG[9]=0xFF;     
   ATMSG[10]=0xFF;     
   ATMSG[11]=0x5A;     
   ATMSG[12]=0x5A;
   senddata(ATMSG,13);    
   while(IsHaveCommand!=C_OK) _NOP();//等待响应指令     
  
   IE2 &= ~UCA0RXIE;
   UC1IE &= ~UCA1RXIE;                       //关闭所有接受
   
   if(MX29LV320t_Flash_Read(1)==0xFF && MX29LV320t_Flash_Read(1000)==0xFF && MX29LV320t_Flash_Read(2000)==0xFF) _NOP();
   else
   {
   MX29LV320t_Cmd_Reset();          //flash的芯片复位
   MX29LV320t_Cmd_Erase_Chip();     //将flash进行芯片擦除
   }
   
   TBCCTL0 = CCIE;                               // CCR0 interrupt enabled
   TBCCR0 = 199;
   TBCTL = TBSSEL_2 + MC_1 + ID_3;
   
   IsCaptureTime=0;
   sum = 0;
   flash_addr=0;   
}  

void MX29LV320t_Cmd_Erase_Chip()  
{  
  MX29LV320t_Command_Write(0xAAA, 0xAA);
  MX29LV320t_Command_Write(0x555, 0x55);  
  MX29LV320t_Command_Write(0xAAA, 0x80);
  MX29LV320t_Command_Write(0xAAA, 0xAA);  
  MX29LV320t_Command_Write(0x555, 0x55);
  MX29LV320t_Command_Write(0xAAA, 0x10);  
  delaymsec();//等待清除完成
  delaymsec();//等待清除完成
  delaymsec();//等待清除完成
  delaymsec();//等待清除完成
  delaymsec();//等待清除完成
  delaymsec();//等待清除完成
  delaymsec();//等待清除完成
} 

void MX29LV320t_Flash_Write(unsigned long vaddress, unsigned char vdata)  
{  
 // int i;
  if(vaddress<0x400000)//flash 最大访问空间为2M×16bits
  {
   
  MX29LV320t_Command_Write(0xAAA, 0xAA);
  MX29LV320t_Command_Write(0x555, 0x55);  
  MX29LV320t_Command_Write(0xAAA, 0xA0);
  MX29LV320t_Command_Write(vaddress, vdata);  
  //延时等待输出Tds=45ns，CPU时钟周期为1/CPU_CLK=83ns
 // for(i=0;i<0x0c;i++);
  
  }
  
} 

unsigned char MX29LV320t_Flash_Read(unsigned long vaddress)
{
  //int i;
  unsigned char tmp=0x0;
  
  if(vaddress<0x4000000)//flash 最大访问空间为2M×16bits
  {
   P1DIR = 0x00;    
  //开始装载地址
   P4OUT = (vaddress >> 1) & 0xFF; //获取A0-A07
   P5OUT = (vaddress >> 9) & 0xFF; //获取A08-A15 
   if(vaddress%2)P2OUT |= 0x80;
     else P2OUT &=~0x80;
   tmp = P2OUT & 0xE0;
   tmp |= (vaddress >> 17) & 0x1F;//获取A16-A20和A-1
   P2OUT =tmp;  
  //使能CE选通信号，低电平有效
  P6OUT &= ~0x10;  
  //使能OE输出信号，低电平有效
  P6OUT &= ~0x40;  
  //延时等待输出Toe=35ns，CPU时钟周期为1/CPU_CLK=83ns,所以无需等待
  //for(i=0;i<100;i++);  
  //获取数据
  tmp = P1IN;  
  //CE OE信号关断
  P6OUT |= 0x50;  
  return tmp;  
  }
  else return 0x0FF;
}

void MX29LV320t_Cmd_Reset(void)
{
 int i=0;
 P6DIR |= 0x80;                             // P6.2 RESET#信号，输出0         
 P6OUT &= ~0x80;
 for(i=0;i<320;i++); //延时500ns 
 P6OUT |= 0x80; // P6.7 RESET#信号，输出1  
 for(i=0;i<320;i++); //延时20us
}

void MX29LV320t_Command_Write(unsigned long vaddress, unsigned char vdata)
{
  //int i;
  unsigned char tmp=0x0;
  
  if(vaddress<0x4000000)//flash 最大访问空间为2M×16bits
  {
   P1DIR = 0xFF;    
  //开始装载地址
   P4OUT = (vaddress >> 1) & 0xFF; //获取A0-A07
   P5OUT = (vaddress >> 9) & 0xFF; //获取A08-A15 
   if(vaddress%2)P2OUT |= 0x80;
     else P2OUT &=~0x80;
   tmp = P2OUT & 0xE0;
   tmp |= (vaddress >> 17) & 0x1F;//获取A16-A20和A-1
   P2OUT =tmp;  
  //使能CE选通信号，低电平有效
  P6OUT &= ~0x10;  
    //装载数据vdata
  P1OUT = vdata;  
  //使能WE输出信号，低电平有效
  P6OUT &= ~0x20;  
  //延时等待输出Toe=35ns，CPU时钟周期为1/CPU_CLK=83ns,所以无需等待
 // for(i=0;i<10;i++); 
  //WE信号关断
  P6OUT |= 0x20;  
  //CE信号关断
  P6OUT |= 0x10;
  }
}

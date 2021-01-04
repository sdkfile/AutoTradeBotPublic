using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
namespace AutoTrade
{
    class Program
    {

        static void Main(string[] args)
        {
            AutoConnect();
            MainAsync().GetAwaiter().GetResult();
            //Test().GetAwaiter().GetResult();
        }

        static void AutoConnect()
        {
            Process.Start("taskkill", "/IM coStarter* /F /T");
            Process.Start("taskkill", "/IM CpStart* /F /T");
            Process.Start("wmic", "process where \"name like \'%coStarter%\'\" call terminate");
            Process.Start("wmic", "process where \"name like \'%CpStart%\'\" call terminate");
            Thread.Sleep(5000);

            Process.Start("C:\\CREON\\STARTER\\coStarter.exe", "/prj:cp /id:abcde /pwd:12345678 /pwdcert:0987654 /autostart");
            Thread.Sleep(120000);
        }
        static async Task MainAsync()
        {
            AutoTradeCore atc = new AutoTradeCore
            {
                isReal = true
            };
            try
            {

                DateTime t_now = DateTime.Now;
                DateTime t_8_40 = new DateTime(t_now.Year, t_now.Month, t_now.Day, 8, 40, 0);
                DateTime t_8_50 = new DateTime(t_now.Year, t_now.Month, t_now.Day, 8, 50, 0, 0);
                DateTime t_9 = new DateTime(t_now.Year, t_now.Month, t_now.Day, 9, 0, 0, 0);
                DateTime t_start = new DateTime(t_now.Year, t_now.Month, t_now.Day, 9, 5, 0, 0);
                DateTime t_sell = new DateTime(t_now.Year, t_now.Month, t_now.Day, 15, 15, 0, 0);
                DateTime t_exit = new DateTime(t_now.Year, t_now.Month, t_now.Day, 15, 20, 0, 0);
                DayOfWeek today = DateTime.Today.DayOfWeek;

                int target_buy_count = AutoTradeCore.JongmokNum; // 매수할 종목 수
                double buy_percent = 1.0 / (target_buy_count); // 일단 반만 해보려고 나눠놓음
                atc.PrintLog("CheckCreonSystem() :" + atc.CheckCreonSystem().ToString()); // 크레온 접속 점검
                int total_cash = (int)await atc.GetCurrentCash(); // 100% 증거금 주문 가능 금액 조회
                atc.CurrentBalance = total_cash;
                int buy_amount = (int)(total_cash * buy_percent); // 종목별 주문 금액 계산
                atc.PrintLog("100% 증거금 주문 가능 금액 :" + total_cash.ToString());
                atc.PrintLog("종목별 주문 금액 :" + buy_amount.ToString());
                atc.PrintLog("시작 시간 :" + DateTime.Now.ToString());
                //bool soldout = false;

                atc.AddExistingStock();
                while (true)
                {
                    t_now = DateTime.Now;

                    if ((today == DayOfWeek.Saturday) || (today == DayOfWeek.Sunday)) // 토요일이나 일요일이면 자동 종료
                    {
                        string weekend = today == DayOfWeek.Saturday ? "Saturday." : "Sunday.";
                        atc.PrintLog("Today is " + weekend);
                        return;
                    }
                    //if (t_9 < t_now && t_now < t_start && soldout == false)
                    //{
                    //    soldout = true;
                    //    await atc.SellAll();
                    //}
                    if (t_9 < t_now && t_now < t_exit) // AM 09:05 ~ PM 03:15 : 매수 & 매도 (매수는 일단 정오까지만 열어놓음)
                    {
                        //await atc.AutoTradeCoreLogicLoop(); // Main Logic
                    }
                    if (t_sell < t_now && t_now < t_exit) // PM 03:15 ~ PM 03:20 : 일괄 매도
                    {
                        if (await atc.SellAll())
                        {
                            await atc.Dbgout("`sell_all() returned True -> self-destructed!`");
                            return;
                        }
                    }
                    if (t_exit < t_now) // PM 03:20 ~ :프로그램 종료
                    {
                        await atc.Dbgout("`self-destructed!`");
                        return;
                    }
                    if (t_now.Minute % 30 == 0 && t_now.Second < 20 && t_now.Second > 5) // 매 30분마다 
                    {
                        await atc.SendCurrentPortfolioMessage();
                    }
                }
            }
            catch (Exception e)
            {
                await atc.Dbgout("`Main -> exception! " + e.ToString() + "`");
            }
        }

        static async Task Test()
        {
            AutoTradeCore atc = new AutoTradeCore
            {
                isReal = false
            };
            try
            {
                DateTime t_now = DateTime.Now;
                DateTime t_8_40 = new DateTime(t_now.Year, t_now.Month, t_now.Day, 8, 40, 0);
                DateTime t_8_50 = new DateTime(t_now.Year, t_now.Month, t_now.Day, 8, 50, 0, 0);
                DateTime t_9 = new DateTime(t_now.Year, t_now.Month, t_now.Day, 9, 0, 0, 0);
                DateTime t_start = new DateTime(t_now.Year, t_now.Month, t_now.Day, 9, 5, 0, 0);
                DateTime t_sell = new DateTime(t_now.Year, t_now.Month, t_now.Day, 15, 15, 0, 0);
                DateTime t_exit = new DateTime(t_now.Year, t_now.Month, t_now.Day, 15, 20, 0, 0);
                DayOfWeek today = DateTime.Today.DayOfWeek;

                int target_buy_count = AutoTradeCore.JongmokNum; // 매수할 종목 수
                double buy_percent = 1.0 / (target_buy_count); // 
                atc.PrintLog("CheckCreonSystem() :" + atc.CheckCreonSystem().ToString()); // 크레온 접속 점검
                int total_cash = (int)await atc.GetCurrentCash(); // 100% 증거금 주문 가능 금액 조회
                atc.CurrentBalance = total_cash;
                int buy_amount = (int)(total_cash * buy_percent); // 종목별 주문 금액 계산
                atc.PrintLog("100% 증거금 주문 가능 금액 :" + total_cash.ToString());
                atc.PrintLog("종목별 주문 금액 :" + buy_amount.ToString());
                atc.PrintLog("시작 시간 :" + DateTime.Now.ToString());
                bool soldout = false;

                while (true)
                {
                    t_now = DateTime.Now;

                    if ((today == DayOfWeek.Saturday) || (today == DayOfWeek.Sunday)) // 토요일이나 일요일이면 자동 종료
                    {
                        string weekend = today == DayOfWeek.Saturday ? "Saturday." : "Sunday.";
                        atc.PrintLog("Today is " + weekend);
                        return;
                    }
                    if (t_9 < t_now && t_now < t_start && soldout == false)
                    {
                        soldout = true;
                        await atc.SellAll();
                    }
                    if (t_start < t_now && t_now < t_exit) // AM 09:05 ~ PM 03:15 : 매수 & 매도
                    {
                        //await atc.AutoTradeCoreLogicLoop(); // Main Logic
                    }
                    if (t_sell < t_now && t_now < t_exit) // PM 03:15 ~ PM 03:20 : 일괄 매도
                    {
                        if (await atc.SellAll())
                        {
                            await atc.Dbgout("`sell_all() returned True -> self-destructed!`");
                            return;
                        }
                    }
                    if (t_exit < t_now) // PM 03:20 ~ :프로그램 종료
                    {
                        await atc.Dbgout("`self-destructed!`");
                        return;
                    }
                    if (t_now.Minute % 30 == 0 && t_now.Second < 20 && t_now.Second > 5) // 매 30분마다 
                    {
                        await atc.SendCurrentPortfolioMessage();
                    }
                }
            }
            catch (Exception e)
            {
                await atc.Dbgout("`Test -> exception! " + e.ToString() + "`");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using SlackAPI;

using CPUTILLib;
using CPTRADELib;
using DSCBO1Lib;
using CPSYSDIBLib;
using CpIndexesLib;
using System.Linq;
using System.Threading;

namespace AutoTrade
{
    partial class AutoTradeCore
    {
        const string TOKEN = "TOKENTOKEN";  // token from last step in section above
        private static readonly SlackTaskClient slackClient = new SlackTaskClient(TOKEN);

        public bool isReal;

        /// <summary>
        /// 인자로 받은 문자열을 커맨드 라인과 슬랙으로 동시에 출력한다.
        /// </summary>
        /// <param name="msg">출력될 메시지</param>
        public async Task Dbgout(string msg)
        {
            string outmsg = $"[{DateTime.Now}] {msg}";
            Console.WriteLine(outmsg);
            var response = await slackClient.PostMessageAsync("#stock", outmsg);
        }

        public async Task RecommendJongmok(string msg)
        {
            string outmsg = $"[{DateTime.Now}] {msg}";
            //Console.WriteLine(outmsg);
            var response = await slackClient.PostMessageAsync("#stock_1", outmsg);
        }

        /// <summary>
        /// 인자로 받은 문자열을 커맨드 라인에 출력한다
        /// </summary>
        /// <param name="msg">출력될 메시지</param>
        public void PrintLog(string msg)
        {
            string outmsg = DateTime.Now.ToString() + " " + msg;
            Console.WriteLine(outmsg);
        }

        // 크레온 플러스 공통 OBJECT
        private CpStockCode _cpCodeMgr;
        private CpCybos _cpStatus;
        private CpTdUtil _cpTradeUtil;
        private StockMst _cpStock;
        private StockChart _cpOhlc;
        public CpSvr7049 _cpStockRank;
        private CpTd6033 _cpBalance;
        private CpTdNew5331A _cpCash;
        private CpTd0311 _cpOrder;
        private CpSeries _cpSeries;
        private CpIndex _cpIndex;

        public Dictionary<string, Jongmok> TrackedJongMoks;

        public List<string> PortfolioList;
        public long CurrentBalance = 200000;
        public long CurrentAsset;
        public double CurrentProfitRatio;

        public AutoTradeCore()
        {
            _cpCodeMgr = new CpStockCode();
            _cpStatus = new CpCybos();
            _cpTradeUtil = new CpTdUtil();
            _cpStock = new StockMst();
            _cpOhlc = new StockChart();
            _cpStockRank = new CpSvr7049();
            _cpBalance = new CpTd6033();
            _cpCash = new CpTdNew5331A();
            _cpOrder = new CpTd0311();
            _cpSeries = new CpSeries();
            _cpIndex = new CpIndex();

            TrackedJongMoks = new Dictionary<string, Jongmok>();
            PortfolioList = new List<string>();
        }

        // 각종 상수
        private const string jongmokPath = "\\jongmok.txt";
        private const int jongmokNum = 4; // 종목 수
        private const double dolpaRatio = 0.3; // k 지수
        private const double harakRatio = 0.3; // 최고가에서 떨어진 비율 
        private const long JongmokPriceHando = 10000; // 종목 1주당 가격 한도
        private const double jasanBiyul = 0.5; // 자산 중 투자 참여 비율

        public static double HarakRatio => harakRatio;

        public static string JongmokPath => jongmokPath;

        public static int JongmokNum => jongmokNum;

        public static long JongmokPriceHando1 => JongmokPriceHando;

        public static double DolpaRatio => dolpaRatio;

        public static double JasanBiyul => jasanBiyul;

        [DllImport("shell32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsUserAnAdmin();

        /// <summary>
        /// 크레온 플러스 시스템 연결 상태를 점검한다.
        /// </summary>
        /// <returns></returns>
        public bool CheckCreonSystem()
        {
            if (!IsUserAnAdmin())
            {
                PrintLog("CheckCreonSystem() : admin user -> FAILED"); // 관리자 권한으로 프로세스 실행 여부
                return false;
            }

            if (_cpStatus.IsConnect == 0)
            {
                PrintLog("CheckCreonSystem() : connect to server -> FAILED"); // 연결 여부 체크
                return false;
            }

            if (_cpTradeUtil.TradeInit(0) != 0)
            {
                PrintLog("CheckCreonSystem() : init trade -> FAILED"); // 주문 관련 초기화 - 계좌 관련 코드가 있을 때만 사용
                return false;
            }
            return true;
        }

        /// <summary>
        /// 인자로 받은 종목의 현재가, 매수호가, 매도호가를 반환한다.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public Tuple<int, int, int> GetCurrentPrice(string code)
        {
            _cpStock.SetInputValue(0, code); // 종목코드에 대한 가격 정보
            _cpStock.BlockRequest();
            Dictionary<string, object> items = new Dictionary<string, object>();
            object CurrentPrice = _cpStock.GetHeaderValue(11); // 현재가
            object AskPrice = _cpStock.GetHeaderValue(16); // 매수호가
            object BidPrice = _cpStock.GetHeaderValue(17); // 매도호가

            return new Tuple<int, int, int>((int)CurrentPrice, (int)AskPrice, (int)BidPrice);
        }

        /// <summary>
        /// 인자로 받은 종목의 OHLC 가격 정보를 qty 개수만큼 반환한다.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public Dictionary<ulong, long[]> GetOhlc(string code, int qty)
        {
            try
            {
                _cpOhlc.SetInputValue(0, code);
                _cpOhlc.SetInputValue(1, '2');
                _cpOhlc.SetInputValue(4, qty);
                _cpOhlc.SetInputValue(5, new int[] { 0, 2, 3, 4, 5 });
                _cpOhlc.SetInputValue(6, 'D');
                _cpOhlc.SetInputValue(9, '1');
                _cpOhlc.BlockRequest();
                long count = (long)_cpOhlc.GetHeaderValue(3);
                Dictionary<ulong, long[]> ohlc = new Dictionary<ulong, long[]>();
                for (int i = 0; i < count; i++)
                {
                    long[] ohlc_val = new long[4];
                    for (int j = 0; j < 4; j++)
                    {
                        ohlc_val[j] = (long)_cpOhlc.GetDataValue(j + 1, i);
                    }
                    //Console.WriteLine($"{(ulong)_cpOhlc.GetDataValue(0, i)} {ohlc_val[0]} {ohlc_val[1]} {ohlc_val[2]} {ohlc_val[3]}");
                    ohlc.Add((ulong)_cpOhlc.GetDataValue(0, i), ohlc_val);
                }
                return ohlc;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        /// <summary>
        /// 인자로 받은 종목의 종목명과 수량을 반환한다. code가 ALL이면 List<Dictionary<string, object>>을 반환, 아닐 경우 Tuple<string, long>을 반환한다.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public object GetStockBalance(string code)
        {
            _cpTradeUtil.TradeInit();
            string acc = ((string[])_cpTradeUtil.AccountNumber)[0];
            var accFlag = (string[])_cpTradeUtil.GoodsList[acc, CPE_ACC_GOODS.CPC_STOCK_ACC];
            _cpBalance.SetInputValue(0, acc);
            _cpBalance.SetInputValue(1, accFlag[0]);
            _cpBalance.SetInputValue(2, 50);
            _cpBalance.BlockRequest();
            if (code == "ALL")
            {
                //await Dbgout("계좌명: " + _cpBalance.GetHeaderValue(0).ToString());
                //await Dbgout("결제잔고수량 : " + _cpBalance.GetHeaderValue(1).ToString());
                //await Dbgout("평가금액: " + _cpBalance.GetHeaderValue(3).ToString());
                //await Dbgout("평가손익: " + _cpBalance.GetHeaderValue(4).ToString());
                //await Dbgout("종목수: " + _cpBalance.GetHeaderValue(7).ToString());
            }
            List<Dictionary<string, object>> stocks = new List<Dictionary<string, object>>();
            for (int i = 0; i < (long)_cpBalance.GetHeaderValue(7); i++)
            {
                string stock_code = (string)_cpBalance.GetDataValue(12, i);
                string stock_name = (string)_cpBalance.GetDataValue(0, i);
                int stock_qty = (int)_cpBalance.GetDataValue(15, i);
                double stock_profit = (double)_cpBalance.GetDataValue(11, i);
                long stock_all_value = (long)_cpBalance.GetHeaderValue(3);
                double stock_all_profit = (double)_cpBalance.GetHeaderValue(8);
                if (code == "ALL")
                {
                    //await Dbgout((i + 1).ToString() + " " + stock_code + "(" + stock_name + ")" + ":" + stock_qty.ToString());
                    stocks.Add(new Dictionary<string, object> {
                        { "code", stock_code },
                        { "name", stock_name },
                        { "qty", stock_qty },
                        { "profit", stock_profit },
                        { "AllBalance", stock_all_value },
                        { "AllProfit", stock_all_profit }
                    });
                }
                if (stock_code == code)
                    return new Tuple<string, long>(stock_name, stock_qty);
            }
            if (code == "ALL")
            {
                return stocks;
            }
            else
            {
                string stock_name = _cpCodeMgr.CodeToName(code);
                return new Tuple<string, long>(stock_name, 0);
            }
        }

        public void AddExistingStock()
        {
            List<Dictionary<string, object>> CurrentStocks = (List<Dictionary<string, object>>)GetStockBalance("ALL");
            PortfolioList = new List<string>();
            foreach (var CurrentStock in CurrentStocks)
            {
                Jongmok jm = new Jongmok((string)CurrentStock["code"]);
                jm.BoughtAmount = (int)CurrentStock["qty"];
                double ratio = (double)CurrentStock["profit"];
                if (jm.HighestProfitRatio < ratio)
                {
                    jm.HighestProfitRatio = ratio;
                }
                jm.CurrentProfitRatio = ratio;
                CurrentAsset = (long)CurrentStock["AllBalance"];
                CurrentProfitRatio = (double)CurrentStock["AllProfit"];
                PortfolioList.Add((string)CurrentStock["code"]);
            }
        }


        /// <summary>
        /// 증거금 100% 주문 가능 금액을 반환한다.
        /// </summary>
        /// <returns></returns>
        public async Task<long> GetCurrentCash()
        {
            try
            {
                _cpTradeUtil.TradeInit();
                string acc = ((string[])_cpTradeUtil.AccountNumber)[0]; // 계좌번호
                string[] accFlag = (string[])_cpTradeUtil.GoodsList[acc, CPE_ACC_GOODS.CPC_STOCK_ACC]; // -1:전체, 1:주식, 2:선물/옵션
                _cpCash.SetInputValue(0, acc); // 계좌번호
                _cpCash.SetInputValue(1, accFlag[0]); // 상품구분 - 주식 상품 중 첫번째
                _cpCash.BlockRequest();
                return _cpCash.GetHeaderValue(47); // 증거금 100% 주문 가능 금액
            }
            catch (Exception e)
            {
                await Dbgout("`GetCurrentCash() -> Error.`" + e.ToString());
                return -1;
            }
        }

        /// <summary>
        /// 매수 목표가를 반환한다.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<double> GetTargetPrice(string code)
        {
            try
            {
                DateTime time_now = DateTime.Now;
                string str_today = time_now.Date.ToString("yyyyMMdd");
                Dictionary<ulong, long[]> ohlc = GetOhlc(code, 20);
                long[] lastday;
                long today_open;
                //Console.WriteLine($"str_today: {str_today}");
                if (str_today == ohlc.Keys.ElementAt(0).ToString())
                {
                    today_open = ohlc.Values.ElementAt(0)[0];
                    lastday = ohlc.Values.ElementAt(1);
                }
                else
                {
                    lastday = ohlc.Values.ElementAt(0);
                    today_open = lastday[3];
                }
                long lastday_high = lastday[1];
                long lastday_low = lastday[2];
                double target_price = today_open + (lastday_high - lastday_low) * DolpaRatio;
                //Console.WriteLine($"Target Price : {target_price}, Today Open : {today_open}, Lastday High : {lastday_high}, Lastday Low: {lastday_low}");

                return target_price;
            }
            catch (Exception e)
            {
                await Dbgout("`get_target_price() -> exception! " + e.ToString() + "`");
                return -1;
            }
        }

        /// <summary>
        /// 인자로 받은 종목에 대한 이동평균가격을 반환한다.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        public async Task<double> GetMovingAverage(string code, int window)
        {
            try
            {
                DateTime time_now = DateTime.Now;
                ulong str_today = (ulong)time_now.Date.Ticks;
                Dictionary<ulong, long[]> ohlc = GetOhlc(code, 20);
                int lastdayIndex;
                if (str_today == ohlc.Keys.ElementAt(0))
                    lastdayIndex = 1;
                else
                    lastdayIndex = 0;
                long[] closes = new long[ohlc.Count];
                for (int i = 0; i < ohlc.Count; i++)
                {
                    closes[i] = ohlc.Values.ElementAt(i)[3];
                }

                double[] buffer = new double[window];
                double[] output = new double[closes.Length];
                int currentIndex = 0;
                for (int i = 0; i < closes.Length; i++)
                {
                    buffer[currentIndex] = (double)closes[i] / (double)window;
                    double ma = 0.0;
                    for (int j = 0; j < window; j++)
                    {
                        ma += buffer[j];
                    }
                    output[i] = ma;
                    currentIndex = (currentIndex + 1) % window;
                }
                return output[lastdayIndex];
            }
            catch (Exception e)
            {
                await Dbgout("GetMovingAverage(" + window.ToString() + ") -> exception! " + e.ToString());
                return -1;
            }

        }

        /// <summary>
        /// 인자로 받은 종목을 최유리 지정가 FOK 조건으로 매수한다.
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<int> buy_stock(string code)
        {
            try
            {
                if (PortfolioList.Contains(code)) // 매수 완료 종목이면 더 이상 안 사도록 함수 종료
                {
                    return -1;
                }
                DateTime time_now = DateTime.Now;
                Tuple<int, int, int> prices = GetCurrentPrice(code);
                int buy_amount = 0;
                long buy_qty = 0; // 매수할 수량 초기화
                if (prices.Item2 > 0)
                {
                    int target_buy_count = JongmokNum; // 매수할 종목 수
                    double buy_percent = 1.0 / (target_buy_count); // 일단 반만 해보려고 나눠놓음
                    int total_cash = (int)await GetCurrentCash(); // 100% 증거금 주문 가능 금액 조회
                    buy_amount = (int)(total_cash * buy_percent); // 종목별 주문 금액 계산
                    buy_qty = buy_amount / prices.Item2;
                }

                PrintLog(TrackedJongMoks[code].name + "(" + code + ") " + buy_qty.ToString() + "EA : " + prices.Item1.ToString() + " meets the buy condition!`");
                _cpTradeUtil.TradeInit();
                string acc = ((string[])_cpTradeUtil.AccountNumber)[0]; // 계좌번호
                var accFlag = (string[])_cpTradeUtil.GoodsList[acc, CPE_ACC_GOODS.CPC_STOCK_ACC]; // -1:전체,1:주식,2:선물/옵션
                // 최유리 FOK 매수 주문 설정
                _cpOrder.SetInputValue(0, "2");        //  1:매도, 2:매수
                _cpOrder.SetInputValue(1, acc);        // 계좌번호
                _cpOrder.SetInputValue(2, accFlag[0]);  // 상품구분 - 주식 상품 중 첫번째
                _cpOrder.SetInputValue(3, code);       // 종목코드
                _cpOrder.SetInputValue(4, buy_qty);    // 매수할 수량
                _cpOrder.SetInputValue(7, "2");        // 주문조건 0:기본, 1:IOC, 2:FOK
                _cpOrder.SetInputValue(8, "12");       // 주문호가 1:보통, 3:시장가, 5:조건부, 12:최유리, 13:최우선
                // 매수 주문 요청
                int ret = (int)_cpOrder.BlockRequest();
                PrintLog($"최유리 FoK 매수 -> {TrackedJongMoks[code].name} | {code} | {buy_qty} -> {ret}");
                if (ret == 4)
                {
                    int remain_time = _cpStatus.LimitRequestRemainTime;
                    PrintLog($"주의: 연속 주문 제한에 걸림. 대기 시간: {(double)remain_time / (double)1000}초");
                    Thread.Sleep(remain_time);
                    return ret;
                }
                else if (ret == 0)
                {
                    TrackedJongMoks[code].BoughtPrice = _cpOrder.GetHeaderValue(5);
                    TrackedJongMoks[code].BoughtAmount = _cpOrder.GetHeaderValue(4);
                    await RecommendJongmok($"현재 추천주 {TrackedJongMoks[code].name}");
                    Thread.Sleep(1000);

                    return ret;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception e)
            {
                await Dbgout("`buy_stock(" + code + ") -> exception! " + e.ToString() + "`");
                return -1;
            }
        }

        /// <summary>
        /// 보유한 모든 종목을 최유리 지정가 IOC 조건으로 매도한다.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> SellAll()
        {
            try
            {
                _cpTradeUtil.TradeInit();
                string acc = ((string[])_cpTradeUtil.AccountNumber)[0]; // 계좌번호
                var accFlag = (string[])_cpTradeUtil.GoodsList[acc, CPE_ACC_GOODS.CPC_STOCK_ACC]; // -1:전체,1:주식,2:선물/옵션
                while (true)
                {
                    List<Dictionary<string, object>> stocks = (List<Dictionary<string, object>>)GetStockBalance("ALL");
                    int total_qty = 0;
                    foreach (var s in stocks)
                    {
                        total_qty += (int)s["qty"];
                    }
                    if (total_qty == 0)
                    {
                        return true;
                    }
                    foreach (var s in stocks)
                    {
                        if ((int)s["qty"] != 0)
                        {
                            _cpOrder.SetInputValue(0, "1"); // 1:매도, 2:매수
                            _cpOrder.SetInputValue(1, acc); // 계좌번호
                            _cpOrder.SetInputValue(2, accFlag[0]); // 주식상품 중 첫번째
                            _cpOrder.SetInputValue(3, (string)s["code"]); // 종목코드
                            _cpOrder.SetInputValue(4, (long)s["qty"]); // 매도 수량
                            _cpOrder.SetInputValue(7, "1"); // 조건 0:기본, 1:IOC, 2:FOK
                            _cpOrder.SetInputValue(8, "12"); // 호가 12:최유리, 13:최우선

                            // 최유리 IOC 매도 주문 요청
                            int ret = _cpOrder.BlockRequest();
                            PrintLog($"최유리 IOC 매도 {s["code"]} | {s["name"]} | {s["qty"]} | -> cpOrder.BlockRequest() -> returned {ret}");
                            if (ret == 4)
                            {
                                int remain_time = _cpStatus.LimitRequestRemainTime;
                                PrintLog($"주의: 연속 주문 제한, 대기시간: {remain_time / 1000}");
                                Thread.Sleep(remain_time);
                            }
                        }
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception e)
            {
                await Dbgout("sell_all() -> exception! " + e.ToString());
                return false;
            }
        }

        public async Task<int> SellStock(string code)
        {
            try
            {
                _cpTradeUtil.TradeInit();
                string acc = ((string[])_cpTradeUtil.AccountNumber)[0]; // 계좌번호
                var accFlag = (string[])_cpTradeUtil.GoodsList[acc, CPE_ACC_GOODS.CPC_STOCK_ACC]; // -1:전체,1:주식,2:선물/옵션
                Jongmok jm = TrackedJongMoks[code];
                while (true)
                {
                    if (jm.BoughtAmount != 0 && jm.BoughtAmount != -1)
                    {
                        _cpOrder.SetInputValue(0, "1"); // 1:매도, 2:매수
                        _cpOrder.SetInputValue(1, acc);         // 계좌번호
                        _cpOrder.SetInputValue(2, accFlag[0]);  // 주식상품 중 첫번째
                        _cpOrder.SetInputValue(3, jm.code); // 종목코드
                        _cpOrder.SetInputValue(4, jm.BoughtAmount);  // 매도수량
                        _cpOrder.SetInputValue(7, "1"); // 조건 0:기본, 1:IOC, 2:FOK
                        _cpOrder.SetInputValue(8, "12"); // 호가 12:최유리, 13:최우선
                        // 최유리 IOC 매도 주문 요청
                        int ret = _cpOrder.BlockRequest();
                        PrintLog($"최유리 IOC 매도 {jm.code} | {jm.name} | {jm.BoughtAmount} | -> cpOrder.BlockRequest() -> returned {ret}");

                        if (ret == 4)
                        {
                            int remain_time = _cpStatus.LimitRequestRemainTime;
                            PrintLog($"주의: 연속 주문 제한, 대기시간: {remain_time / 1000}");
                            Thread.Sleep(remain_time);
                            return 4;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                            return 0;
                        }
                    }
                    Thread.Sleep(1000);
                    return -1;
                }
            }
            catch (Exception e)
            {
                await Dbgout($"`SellStock({code})->exception! `" + e.ToString());
                return -1;
            }
        }
    }
}

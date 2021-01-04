using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CpIndexesLib;
using CPSYSDIBLib;
using CPUTILLib;
using DSCBO1Lib;

namespace AutoTrade
{
    public class Jongmok
    {
        public string code; // 종목 코드
        public string name; // 종목 이름

        public int BoughtPrice; //산 가격
        public int BoughtAmount; // 산 갯수
        public int SoldPrice; // 판 가격

        public int YesterClosedPrice; // 전일 종가
        public int CurrentPrice; // 현재가
        public int StartPrice; // 시가
        public int HighestPrice; // 금일 최고가
        public int LowestPrice; // 금일 최저가
        public int BidPrice; // 매도호가
        public int AskPrice; // 매수호가
        public int AccTrStock; // 누적 거래량
        public int AccTrMoney; // 누적 거래대금
        public float PER; // Profit-Earning Rate

        //public double TargetPrice;
        public bool DidHadJongmok = false; // 오늘 가지고 있었는가
        public int ChoiGoGaTillNow = 0; // 지금까지의 최고가
        public double TargetPrice; // 돌파 타겟 가격

        public double ma5Price; // 5일 이동평균
        public double ma10Price; // 10일 이동평균
        public double ma20Price; // 20일 이동평균
        public double ma60Price; // 60일 이동평균

        public double LastClosePrice; // 직전구획 종가

        public Queue<double> macd12_26; // MACD12-26
        public Queue<double> macdSignal_9; // MACD Signal9
        public Queue<double> macd_OSC; // MACD Oscillator

        public Queue<double> SlowK5; // Slow %K_5
        public Queue<double> SlowD3; // Slow %D_3

        public Queue<double> CCI14; // CCI14
        public Queue<double> CCISignal_9; // CCI Signal9

        public Queue<double> RSI; // RSI
        public Queue<double> RSI_sig; // RSI Signal

        public Queue<double> BolUp; // 볼린저 밴드 상
        public Queue<double> BolDown; // 볼린저 밴드 하
        public Queue<double> BolMid; // 볼린저 밴드 중
        public double PB;

        public Queue<double> MFI; // MFI
        public Queue<double> MFI_sig; // MFI Signal

        public Queue<double> Vol_5ma; // 거래량 5ma

        public double HighestProfitRatio = 0; // 가지고 있는 동안 최대 수익률
        public double CurrentProfitRatio; // 현재 수익률


        public Jongmok(string code)
        {
            try
            {
                _cpStock = new StockMst();
                _cpSeries = new CpSeries();
                _cpOhlc = new StockChart();
                _cpIndex = new CpIndex();
                _cpStatus = new CpCybos();

                macd12_26 = new Queue<double>();
                macdSignal_9 = new Queue<double>();
                macd_OSC = new Queue<double>();
                SlowK5 = new Queue<double>();
                SlowD3 = new Queue<double>();
                CCI14 = new Queue<double>();
                CCISignal_9 = new Queue<double>();
                RSI = new Queue<double>();
                RSI_sig = new Queue<double>();
                BolUp = new Queue<double>();
                BolDown = new Queue<double>();
                BolMid = new Queue<double>();
                MFI = new Queue<double>();
                MFI_sig = new Queue<double>();
                Vol_5ma = new Queue<double>();

                TargetPrice = GetTargetPrice(code);

                UpdateJM(code);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void UpdateJM(string code)
        {
            try
            {
                _cpStock.SetInputValue(0, code); // 종목코드에 대한 가격 정보
                _cpStock.BlockRequest();
                this.code = _cpStock.GetHeaderValue(0);
                this.name = _cpStock.GetHeaderValue(1);
                this.YesterClosedPrice = _cpStock.GetHeaderValue(10); // 전일종가
                this.CurrentPrice = _cpStock.GetHeaderValue(11); // 현재가
                this.StartPrice = _cpStock.GetHeaderValue(13); // 시가
                this.HighestPrice = _cpStock.GetHeaderValue(14); // 고가
                this.LowestPrice = _cpStock.GetHeaderValue(15); // 저가
                this.BidPrice = _cpStock.GetHeaderValue(16); // 매도호가
                this.AskPrice = _cpStock.GetHeaderValue(17); // 매수호가
                this.AccTrStock = _cpStock.GetHeaderValue(18); // 누적 거래량
                this.AccTrMoney = _cpStock.GetHeaderValue(19); // 누적 거래대금
                this.PER = _cpStock.GetHeaderValue(28); // PER

                if (ChoiGoGaTillNow < CurrentPrice)
                {
                    ChoiGoGaTillNow = CurrentPrice;
                }
                WaitForLimitationTime();

                ////Request(code, 100, _cpSeries);
                //var Rq = Request(code, 100, _cpSeries, 'D');
                //this.ma5Price = (double)MakeIndex(code, "이동평균(라인1개)", Rq, 5)["이동평균"];
                //this.ma10Price = (double)MakeIndex(code, "이동평균(라인1개)", Rq, 10)["이동평균"];
                //this.ma20Price = (double)MakeIndex(code, "이동평균(라인1개)", Rq, 20)["이동평균"];
                //this.ma60Price = (double)MakeIndex(code, "이동평균(라인1개)", Rq, 60)["이동평균"];

                //var Rq1 = Request(code, 100, _cpSeries, 'm');
                //var macd = MakeIndex(code, "MACD", Rq1);
                //EnqueueIndexLine((double)macd["MACD"], macd12_26);
                //EnqueueIndexLine((double)macd["SIGNAL"], macdSignal_9);
                //EnqueueIndexLine((double)macd["OSCILLATOR"], macd_OSC);

                //var cci = MakeIndex(code, "CCI", Rq1);
                //EnqueueIndexLine((double)cci["CCI"], CCI14);
                //EnqueueIndexLine((double)cci["SIGNAL"], CCISignal_9);

                //var slow = MakeIndex(code, "Stochastic Slow", Rq1);
                //EnqueueIndexLine((double)slow["SLOW K"], SlowK5);
                //EnqueueIndexLine((double)slow["SLOW D"], SlowD3);

                //var rsi = MakeIndex(code, "RSI", Rq1);
                //EnqueueIndexLine((double)rsi["RSI"], RSI);
                //EnqueueIndexLine((double)rsi["SIGNAL"], RSI_sig);

                //var bollinger = MakeIndex(code, "Bollinger Band", Rq1);
                //EnqueueIndexLine((double)bollinger["Bol상"], BolUp);
                //EnqueueIndexLine((double)bollinger["Bol하"], BolDown);
                //EnqueueIndexLine((double)bollinger["Bol중"], BolMid);
                //PB = (LastClosePrice - (double)bollinger["Bol하"]) / ((double)bollinger["Bol상"] - (double)bollinger["Bol하"]);

                //var mfi = MakeIndex(code, "MFI", Rq1);
                //EnqueueIndexLine((double)mfi["MFI"], MFI);
                //EnqueueIndexLine((double)mfi["SIGNAL"], MFI_sig);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
        public CpSeries Request(string code, int cnt, CpSeries _cpSeries, char unit = 'D')
        {
            // 1. 일간 차트 데이터 요청
            _cpOhlc.SetInputValue(0, code); // 종목 코드
            _cpOhlc.SetInputValue(1, '2'); // 개수로 조회
            _cpOhlc.SetInputValue(4, cnt); // 최근 cnt일치
            _cpOhlc.SetInputValue(5, new int[] { 0, 2, 3, 4, 5, 8 }); // 날짜, 시가, 고가, 저가, 종가, 거래량
            _cpOhlc.SetInputValue(6, unit); // 차트 주기 - 일간 차트 요청
            _cpOhlc.SetInputValue(7, 10);
            _cpOhlc.SetInputValue(9, '1'); // 수정주가 사용
            _cpOhlc.BlockRequest();

            var rqStatus = _cpOhlc.GetDibStatus();
            var rqRet = _cpOhlc.GetDibMsg1();
            //Console.WriteLine($"통신상태 {rqStatus} | {rqRet}");
            if (rqStatus != 0)
            {
                return null;
            }
            // 2. 일간 차트 데이터 => CpIndexes.CpSeries로 변환
            int len = _cpOhlc.GetHeaderValue(3);
            double volavg = 0;
            for (int i = 0; i < len; i++)
            {
                int index = len - i - 1;
                var day = _cpOhlc.GetDataValue(0, index);
                var open = _cpOhlc.GetDataValue(1, index);
                var high = _cpOhlc.GetDataValue(2, index);
                var low = _cpOhlc.GetDataValue(3, index);
                var close = _cpOhlc.GetDataValue(4, index);
                var vol = _cpOhlc.GetDataValue(5, index);
                // _cpSeries에 종가 시가 고가 저가 거래량 저장
                _cpSeries.Add(close, open, high, low, vol);
                LastClosePrice = close;
                if (i >= len - 5)
                {
                    volavg += vol;
                }
            }
            volavg /= 5;
            EnqueueIndexLine(volavg, Vol_5ma);
            WaitForLimitationTime();
            return _cpSeries;
        }

        private Dictionary<string, object> MakeIndex(string code, string indexName, CpSeries cpSeries, int maNum = 5)
        {
            var lineName = this.IndexList[indexName];
            var chartvalue = new Dictionary<string, object>();
            _cpIndex.Series = cpSeries;
            _cpIndex.put_IndexKind(indexName);
            _cpIndex.put_IndexDefault(indexName);
            if (indexName == "이동평균(라인1개)" && maNum != 5)
            {
                _cpIndex.Term1 = maNum;
            }

            // 지표 데이터 계산하기
            _cpIndex.Calculate();
            var cntOfIndex = _cpIndex.ItemCount;
            // 지표의 각 라인 이름은 HTS 차트의 각 지표 조건 참고
            for (int i = 0; i < cntOfIndex; i++)
            {
                var name = lineName[i];
                chartvalue[name] = new List<object>();
                var cnt = _cpIndex.GetCount(i);
                for (int j = 0; j < cnt; j++)
                {
                    var value = _cpIndex.GetResult(i, j);
                    chartvalue[name] = value;
                }
            }
            return chartvalue;
        }

        #region Utility
        public void WaitForLimitationTime()
        {
            if (_cpStatus.GetLimitRemainCount(LIMIT_TYPE.LT_NONTRADE_REQUEST) == 5)
            {
                Console.WriteLine($"대기중... 남은시간 {(double)(_cpStatus.LimitRequestRemainTime) / (double)1000}초");
                Thread.Sleep(_cpStatus.LimitRequestRemainTime + 1000);
            }
        }

        public void ListAllIndex()
        {
            _cpIndex = new CpIndex();
            var allIndexList = new List<object>();
            for (int i = 0; i < 7; i++)
            {
                var hihi = (object[])_cpIndex.GetChartIndexCodeListByIndex(i);
                foreach (var h in hihi)
                {
                    allIndexList.Add(h);
                }
            }
            foreach (var index in allIndexList)
            {
                _cpIndex.put_IndexKind(index);
                _cpIndex.put_IndexKind(index);

                Console.WriteLine($"{index} : 변수1 {_cpIndex.get_Term1()} 변수2 {_cpIndex.get_Term2()} 변수3 {_cpIndex.get_Term3()} 변수4 {_cpIndex.get_Term4()} Signal {_cpIndex.get_Signal()}");
            }
        }

        private void EnqueueIndexLine(double input, Queue<double> q)
        {
            if (q.Count == 0)
            {
                q.Enqueue(input);
            }
            else
            {
                if (q.ElementAt(q.Count - 1) != input && Math.Abs(q.ElementAt(q.Count - 1) - input) > 0.0001)
                {
                    q.Enqueue(input);
                }
                if (q.Count > 5)
                {
                    q.Dequeue();
                }
            }
        }

        public double GetTargetPrice(string code)
        {
            try
            {
                DateTime time_now = DateTime.Now;
                string str_today = time_now.Date.ToString("yyyyMMdd");
                Dictionary<ulong, long[]> ohlc = GetOhlc(code, 20);
                long[] lastday;
                long today_open;
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
                double target_price = today_open + (lastday_high - lastday_low) * AutoTradeCore.DolpaRatio;

                return target_price;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return -1;
            }
        }

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
                WaitForLimitationTime();
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

        #endregion

        // 크레온 플러스 공통 OBJECT
        private CpCybos _cpStatus;
        private StockMst _cpStock;
        private StockChart _cpOhlc;
        private CpSeries _cpSeries;
        private CpIndex _cpIndex;

        private Dictionary<string, string[]> IndexList = new Dictionary<string, string[]>() {
            { "지표선택 없음", new string[] { "없음" } },
            { "이동평균(라인1개)", new string[] { "이동평균" } },
            { "Stochastic Slow", new string[] { "SLOW K", "SLOW D" } },
            { "MACD", new string[] { "MACD", "SIGNAL", "OSCILLATOR" } },
            { "RSI", new string[] { "RSI", "SIGNAL" } },
            { "Binary Wave MACD", new string[] { "BWMACD", "SIGNAL", "OSCILLATOR" } },
            { "TSF", new string[] { "TSF", "SIGNAL" } },
            { "ZigZag", new string[] { "ZigZag1", "ZigZag2" } },
            { "Bollinger Band", new string[] { "Bol상", "Bol하", "Bol중" } },
            { "CCI", new string[] { "CCI", "SIGNAL"} },
            { "MFI", new string[] { "MFI", "SIGNAL"} }
        };
    }
}

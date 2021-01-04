using CPUTILLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoTrade
{
    partial class AutoTradeCore
    {
        public bool IsInCreasing(Queue<double> q)
        {
            if (q.Count < 2)
                return false;
            return q.ElementAt(q.Count - 2) < q.Last();
        }
        public bool IsDecreasing(Queue<double> q)
        {
            if (q.Count < 2)
                return false;
            return q.ElementAt(q.Count - 2) > q.Last();
        }
        public bool RightUpward(Queue<double> q)
        {
            if (q.Count < 2)
                return true;
            return (q.ElementAt(q.Count - 2) < 0) && (q.Last() > 0);
        }

        public bool LeftDownward(Queue<double> q)
        {
            if (q.Count < 2)
                return false;
            return (q.ElementAt(q.Count - 2) > 0) && (q.Last() < 0);
        }

        public bool VagueRightUpward(Queue<double> q)
        {
            bool bool1 = q.Last() > 0;
            bool bool2;
            bool bool3;
            if (q.Count < 3)
            {
                bool2 = true;
                bool3 = true;
            }
            else
            {
                bool2 = q.ElementAt(q.Count - 3) < 0;
                bool3 = Math.Abs(q.ElementAt(q.Count - 3) + q.ElementAt(q.Count - 2) + q.Last()) < 5;
            }
            return bool1 && bool2 && bool3;
        }

        public bool VagueLeftDownward(Queue<double> q)
        {
            bool bool1 = q.Last() < 0;
            bool bool2;
            bool bool3;
            if (q.Count < 3)
            {
                bool2 = true;
                bool3 = true;
            }
            else
            {
                bool2 = q.ElementAt(q.Count - 3) > 0;
                bool3 = Math.Abs(q.ElementAt(q.Count - 3) + q.ElementAt(q.Count - 2) + q.Last()) < 5;
            }
            return bool1 && bool2 && bool3;
        }

        public double? Slope(Queue<double> q)
        {
            if (q.Count < 2)
            {
                return null;
            }
            else
            {
                return q.Last() - q.ElementAt(q.Count - 2);
            }
        }
        public bool? SlopTurnedUp(Queue<double> q)
        {
            if (q.Count < 3)
            {
                return null;
            }
            else
            {
                return (q.Last() - q.ElementAt(q.Count - 2) > 10) && (q.ElementAt(q.Count - 2) - q.ElementAt(q.Count - 3) < 0);
            }
        }
        public bool? SlopeTurnedDown(Queue<double> q)
        {
            if (q.Count < 3)
            {
                return null;
            }
            else
            {
                return (q.Last() - q.ElementAt(q.Count - 2) < -10) && (q.ElementAt(q.Count - 2) - q.ElementAt(q.Count - 3) > 0);
            }
        }


        public void WaitForLimitationTime()
        {
            if (_cpStatus.GetLimitRemainCount(LIMIT_TYPE.LT_NONTRADE_REQUEST) == 5)
            {
                Console.WriteLine($"대기중... 남은시간 {(double)(_cpStatus.LimitRequestRemainTime) / (double)1000}초");
                Thread.Sleep(_cpStatus.LimitRequestRemainTime + 1000);
            }
        }

        public async Task SendCurrentPortfolioMessage()
        {
            string outl = "";
            outl += "현재 포트폴리오\n";
            long money = CurrentAsset;
            foreach (var s in PortfolioList)
            {
                var jm = TrackedJongMoks[s];
                outl += $"{jm.name} : {jm.BoughtAmount}주, 손익비율 {jm.CurrentProfitRatio}%\n";
            }
            outl += $"총 평가금액 : {money}원\n";
            outl += $"총 수익율 : {CurrentProfitRatio}%\n";
            await Dbgout(outl);
        }

        public void UpdateCurrentPortfolio()
        {
            List<Dictionary<string, object>> StockBalanceNow = (List<Dictionary<string, object>>)GetStockBalance("ALL");
            PortfolioList = new List<string>();
            foreach (var st in StockBalanceNow)
            {
                Jongmok jm;
                if (TrackedJongMoks.ContainsKey((string)st["code"]))
                {
                    jm = TrackedJongMoks[(string)st["code"]];
                }
                else
                {
                    jm = new Jongmok((string)st["code"]);
                    TrackedJongMoks.Add(jm.code, jm);
                }
                jm.BoughtAmount = (int)st["qty"];
                double ratio = (double)st["profit"];
                if (jm.HighestProfitRatio < ratio)
                {
                    jm.HighestProfitRatio = ratio;
                }
                jm.CurrentProfitRatio = ratio;
                CurrentAsset = (long)st["AllBalance"];
                CurrentProfitRatio = (double)st["AllProfit"];
                PortfolioList.Add((string)st["code"]);
            }
        }
    }
}

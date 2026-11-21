using System;

namespace XiaoheiDesktop
{
    // Companion state is deliberately independent from rendering, timers and persistence.
    public sealed class CompanionModel
    {
        private readonly Func<DateTime> clock;
        private int affection = 35;
        private int snacks;
        private int pets;
        private double energy = 85;
        private string status = "我在这儿，陪你慢慢来。";
        private DateTime lastStroke = DateTime.MinValue;
        private DateTime lastFeed = DateTime.MinValue;
        private DateTime lastPlay = DateTime.MinValue;
        private DateTime lastTick;
        private DateTime lastInteraction;

        public CompanionModel() : this(delegate { return DateTime.Now; }) { }

        public CompanionModel(Func<DateTime> clock)
        {
            if (clock == null) throw new ArgumentNullException("clock");
            this.clock = clock;
            StartedAt = clock();
            lastTick = StartedAt;
            lastInteraction = StartedAt;
        }

        public int Affection
        {
            get { return affection; }
            set { affection = Math.Max(0, Math.Min(100, value)); }
        }

        public int Snacks
        {
            get { return snacks; }
            set { snacks = Math.Max(0, value); }
        }

        public int Pets
        {
            get { return pets; }
            set { pets = Math.Max(0, value); }
        }

        public double Energy
        {
            get { return energy; }
            set { energy = Double.IsNaN(value) ? 85 : Math.Max(0, Math.Min(100, value)); }
        }

        public DateTime StartedAt { get; set; }
        public bool Sleeping { get; set; }
        public bool Quiet { get; set; }
        public bool Roaming { get; set; }

        public string Status
        {
            get { return status; }
            set { status = value ?? String.Empty; }
        }

        public string Stroke()
        {
            DateTime now = clock();
            if (CoolingDown(now, lastStroke, 0.6))
                return Sleeping ? "呼噜……" : "呼噜呼噜，好舒服。";
            lastStroke = now;
            lastInteraction = now;
            if (Pets < Int32.MaxValue) Pets++;
            Affection += 1;
            Energy += 0.5;
            if (Sleeping) return Status = "呼噜……梦里也收到摸摸了。";
            string[] lines = {
                "呼噜呼噜，再摸一下？",
                "今天也收到你的温柔啦。",
                "喵。陪你待着就很好。",
                "耳朵也可以轻轻摸一下。",
                "你忙你的，我负责可爱。"
            };
            return Status = lines[(Pets - 1) % lines.Length];
        }

        public string Feed()
        {
            DateTime now = clock();
            if (CoolingDown(now, lastFeed, 25))
                return "小鱼干还在慢慢嚼，等一会儿再来。";
            lastFeed = now;
            lastInteraction = now;
            if (Snacks < Int32.MaxValue) Snacks++;
            Affection += 2;
            Energy += 3;
            return Status = Sleeping
                ? "小鱼干放好啦，睡醒再吃。"
                : (Snacks % 2 == 0 ? "咔嚓咔嚓……这份小鱼干很香！" : "小鱼干！谢谢招待，喵。 ");
        }

        public string Play()
        {
            DateTime now = clock();
            if (CoolingDown(now, lastPlay, 8))
                return "刚刚那一下真开心，让尾巴歇两秒。";
            lastPlay = now;
            lastInteraction = now;
            bool wasSleeping = Sleeping;
            Sleeping = false;
            Affection += 2;
            Energy -= 6;
            return Status = wasSleeping
                ? "伸个懒腰，陪你玩一会儿！"
                : "出击！抓住这根逗猫棒。";
        }

        public string ToggleSleep()
        {
            Sleeping = !Sleeping;
            lastInteraction = clock();
            return Status = Sleeping
                ? "蜷成一小团，安静陪着你。"
                : "睡饱了！今天也一起慢慢来。";
        }

        public void Tick(DateTime now)
        {
            double elapsed = (now - lastTick).TotalSeconds;
            lastTick = now;
            // Bounded catch-up handles suspend, system-clock corrections and timer delays.
            if (elapsed <= 0) return;
            elapsed = Math.Min(elapsed, 3600);
            Energy += elapsed * (Sleeping ? 0.25 : 0.02);
            if (!Sleeping && (now - lastInteraction).TotalMinutes >= 10)
                Status = "我在这儿，你按自己的节奏来。";
        }

        private static bool CoolingDown(DateTime now, DateTime previous, double seconds)
        {
            double elapsed = (now - previous).TotalSeconds;
            return elapsed >= 0 && elapsed < seconds;
        }
    }
}

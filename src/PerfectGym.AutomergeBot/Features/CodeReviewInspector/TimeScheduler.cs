using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;

namespace PerfectGym.AutomergeBot.Features.CodeReviewInspector
{
    public class TimeScheduler
    {
        
        public List<ScheduleAction> ScheduledActions { get;  } =new List<ScheduleAction>();
        

        Timer timer = new Timer();

        private DateTime _lastCheck;

        public void Start()
        {
            _lastCheck=DateTime.Now;
            timer.Interval = TimeSpan.FromMinutes(1).TotalMilliseconds;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;

             var actions= GetActionsToIvoke(_lastCheck, now);

            _lastCheck = now;

            foreach (var action in actions)
            {
                action.Action();
            }

            
        }

        private List<ScheduleAction> GetActionsToIvoke(DateTime from, DateTime to)
        {
            if (from.Day == to.Day)
            {
                return ScheduledActions.Where(a => from.TimeOfDay <= a.Time && a.Time < to.TimeOfDay).ToList();
            }
            else
            {
                return ScheduledActions.Where(a => from.TimeOfDay <= a.Time || a.Time < to.TimeOfDay).ToList();
            }

        }


        public class ScheduleAction
        {
            public ScheduleAction(TimeSpan time, Action action)
            {
                Time = time;
                Action = action;
            }

            public TimeSpan Time { get; set; }
            public Action Action { get; set; }
        }
    }
}

using System;

namespace Todolist.Helpers
{
    public class Calc
    {
        public static int WeekEstimateCalc(int TaskEstimate = 0,int WeekEstimate = 0,int SumOfGoalTasksEstimates = 0) 
        {
            int result = 0;
            if(SumOfGoalTasksEstimates < WeekEstimate)
                result = TaskEstimate;
            else 
            {
                double TaskShare = (double)TaskEstimate / (double)SumOfGoalTasksEstimates;
                result = (int)Math.Round(TaskShare * WeekEstimate);
            }
            return result;
        }
        public static int genRandomNumber(int seed = 26)
        {
            Random r = new Random();
            return r.Next(seed);
        }
    }
}

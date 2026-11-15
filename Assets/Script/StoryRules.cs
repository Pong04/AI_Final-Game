public static class StoryRules
{
    public const int FullHearts = 3;
    public const int ExcellentScore = 350;

    public static Grade GradeForHearts(int hearts)
    {
        return hearts >= FullHearts ? Grade.A : hearts > 0 ? Grade.B : Grade.F;
    }

    public static int PointsForHearts(int hearts)
    {
        switch (hearts)
        {
            case 3: return 100;
            case 2: return 80;
            case 1: return 50;
            default: return 20;
        }
    }

    public static int FinalEnding(int totalScore, int professorAffinity)
    {
        if (totalScore <= 210) return 2;
        if (totalScore >= ExcellentScore && professorAffinity >= 2) return 3;
        return 1;
    }
}

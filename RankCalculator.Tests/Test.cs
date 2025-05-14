using Xunit;

namespace RankCalculator.Tests
{
    public class RankTestData : TheoryData<string, double>
    {
        public RankTestData()
        {
            Add("", 0.00);
            Add("abc", 0.00); 
            Add("123", 1.00);
            Add("!@#", 1.00);
            Add("абв", 0.00);
            Add("abc123", 0.50);
            Add("abc 12", 0.50);
            Add("😀", 1.00);
            Add("😀abc", 0.25);
            Add("😀 abc", 0.40);
        }
    }

    public class RankCalculatorTests
    {
        [Theory]
        [ClassData(typeof(RankTestData))]
        public void Test_RankCalculate(string input, double expected)
        {
            double result = RankCalculator.Calculate(input);
            Assert.Equal(expected, result, precision: 2);
        }
    }
}
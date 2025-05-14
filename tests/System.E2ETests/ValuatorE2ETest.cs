using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI; 
using System.IO;
using System.Text.Json;
using System.Globalization;
using Xunit;

namespace System.E2ETests;

public class ValuatorE2ETests : IDisposable
{
    private readonly IWebDriver _driver;
    private readonly TestData[] _cases;

    public ValuatorE2ETests()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "TestData.json"));
        _cases = System.Text.Json.JsonSerializer.Deserialize<TestData[]>(json, new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        })!;
        
        var options = new ChromeOptions();

        _driver = new ChromeDriver(options);
    }

    [Fact]
    public void SubmitTextAndVerifyRankAndSimilarity_ForAllCases()
    {
        foreach (var tc in _cases)
        {
            _driver.Navigate().GoToUrl("http://localhost:8000");
            
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            var textarea = wait.Until(drv => drv.FindElement(By.Id("text")));
            textarea.Clear();
            textarea.SendKeys(tc.Text);

            var selectElement = wait.Until(drv => drv.FindElement(By.Id("country")));
            var select = new SelectElement(selectElement);
            
            try
            {
                var option = wait.Until(drv => drv.FindElement(By.XPath($"//option[text()='{tc.Country}']")));
                option.Click();
            }
            catch (NoSuchElementException)
            {
                Assert.True(false, $"Option with country name '{tc.Country}' not found");
            }
            _driver.FindElement(By.CssSelector("button[type=submit]")).Click();
            
            var rankTextElement = wait.Until(drv => drv.FindElement(By.XPath("//p[contains(text(),'Оценка содержания')]")));
            var simTextElement = wait.Until(drv => drv.FindElement(By.XPath("//p[contains(text(),'Плагиат')]")));
            
            wait.Until(drv => !rankTextElement.Text.Contains("не завершена"));
            wait.Until(drv => !simTextElement.Text.Contains("не завершена"));

            var rankText = rankTextElement.Text;
            var simText  = simTextElement.Text;
            
            double actualRank = 0;
            if (!rankText.Contains("не завершена"))
            {
                actualRank = double.Parse(rankText.Split(':')[1].Trim(), CultureInfo.InvariantCulture);
            }
            
            double actualSim = 0;
            if (!simText.Contains("не завершена"))
            {
                actualSim = double.Parse(simText.Split(':')[1].Trim(), CultureInfo.InvariantCulture); 
            }

            Assert.Equal(tc.ExpectedRank, actualRank, 2);
            Assert.Equal(tc.ExpectedSimilarity, actualSim, 2);
        }
    }

    public void Dispose() => _driver.Quit();
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ryokohbato_github_contributions
{
  class Program
  {
    private static Slack _slack = new Slack();
    public static async Task Main(string[] args)
    {
      var result = await GetContributionsAsync();
      var contributions
        = result.RootElement
            .GetProperty("data")
            .GetProperty("viewer")
            .GetProperty("contributionsCollection")
            .GetProperty("contributionCalendar")
            .GetProperty("weeks");

      var targetDate = DateTime.Now;
      targetDate = targetDate.AddDays(-1);

      for (int i = contributions.GetArrayLength() - 1; i >= 0; i --)
      {
        var weekContributions = contributions[i].GetProperty("contributionDays");
        for (int j = 0; j < weekContributions.GetArrayLength(); j ++)
        {
          if (weekContributions[j].GetProperty("date").ToString() == $"{targetDate.ToString("yyyy-MM-dd")}")
          {
            var res = await PostToSlackAsync(weekContributions[j].GetProperty("contributionCount").ToString(), targetDate);
          }
        }
      }
    }

    public static async Task<JsonDocument> GetContributionsAsync()
    {
      var httpClient = new HttpClient
      {
        BaseAddress = new Uri("https://api.github.com/graphql")
      };

      httpClient.DefaultRequestHeaders.Add("User-Agent", "ryokohbato-github-contributions");
      httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SecretData.GitHub.AccessToken);

      var query = new
      {
        query = @"
        {
          viewer {
            login
            contributionsCollection {
              contributionCalendar {
                weeks {
                  contributionDays {
                    contributionCount
                    date
                  }
                }
              }
            }
          }
        }",
        variables = new { }
      };

      var request = new HttpRequestMessage
      {
        Method = HttpMethod.Post,
        Content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json")
      };

      dynamic result;

      using (var response = await httpClient.SendAsync(request))
      {
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        result = System.Text.Json.JsonDocument.Parse(responseString);
      }

      return result;
    }

    private async static Task<bool> PostToSlackAsync(string contributionCount, DateTime date)
    {
      return await _slack.PostJsonMessageAsync(@"
      {
        'channel': 'ryokohbato-life',
        'blocks': [
          {
            'type': 'section',
            'text': {
              'type': 'mrkdwn',
              'text': '<@ryokohbato>\n" + date.ToString("M/d") + "のGitHub Contribution数は" + contributionCount + @"だよ！'
            }
          }
        ]
      }", SecretData.Slack.BotUserOAuthToken);
    }
  }
}

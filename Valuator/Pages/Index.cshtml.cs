using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IDatabase _db;

    private double CalcRank(string text)
    {
        double noNormal = 0.0;
        double result = 0.0;

        if (text.Length == 0)
        {
            return 0.0;
        }

        foreach (char value in text)
        {
            if (!char.IsLetter(value))
            {
                noNormal++;
            }
        }

        result = noNormal / text.Length;

        return result;
    }

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _db = redis.GetDatabase();
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Page();
        }

        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();

        string textKey = "TEXT-" + id;
        // TODO: (pa1) сохранить в БД (Redis) text по ключу textKey
        _db.StringSet(textKey, text);


        string rankKey = "RANK-" + id;
        // TODO: (pa1) посчитать rank и сохранить в БД (Redis) по ключу rankKey
        double rankValue = Math.Round(CalcRank(text), 4);
        _db.StringSet(rankKey, rankValue);


        string similarityKey = "SIMILARITY-" + id;
        // TODO: (pa1) посчитать similarity и сохранить в БД (Redis) по ключу similarityKey
        const string allTextsKey = "ALL_TEXTS";

        bool added = _db.SetAdd(allTextsKey, text);
        int similarity = added ? 0 : 1;

        _db.StringSet(similarityKey, similarity);

        return Redirect($"summary?id={id}");
    }
}
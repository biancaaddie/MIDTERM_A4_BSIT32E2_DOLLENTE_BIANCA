using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RandomQuoteApi.Data;
using RandomQuoteApi.Models;
using System;

namespace RandomQuoteApi.Controllers;

[ApiController]
[Route("[controller]")] // Maps to /Quotes
public class QuotesController : ControllerBase
{
    private readonly AppDbContext _context;

    public QuotesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuoteDto>>> GetQuotes([FromQuery] string? category)
    {

        var query = _context.Quotes.Include(q => q.Category).AsQueryable();

        if (!string.IsNullOrEmpty(category) && category != "all")
        {
            query = query.Where(q => q.Category!.Name == category);
        }

        var quotes = await query.ToListAsync();

        var dtos = quotes.Select(q => new QuoteDto
        {
            Id = q.Id,
            Text = q.Text,
            Author = q.Author,
            Category = q.Category!.Name
        });
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<QuoteDto>> CreateQuote(CreateQuoteDto dto)
    {
        // 1. Validation
        if (string.IsNullOrWhiteSpace(dto.Text) || string.IsNullOrWhiteSpace(dto.Category))
        {
            return BadRequest("Text and Category are required.");
        }
        var categoryName = dto.Category.Trim().ToLower();

        // 2. Check if Category exists in DB
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == categoryName);

        // 3. Fail if category doesn't exist (Enforcement)
        if (category == null)
        {
            return BadRequest($"Category '{dto.Category}' does not exist. Please use: sweet, funny, dark, sarcastic.");
        }
        // 4. Create the Quote Entity
        var quote = new Quote
        {

            Text = dto.Text,
            Author = dto.Author,
            Category = category
        };

        // 5. Save to Database
        _context.Quotes.Add(quote);
        await _context.SaveChangesAsync();

        // 6. Return the created DTO
        var responseDto = new QuoteDto
        {
            Id = quote.Id,
            Text = quote.Text,
            Author = quote.Author,
            Category = category.Name
        };
        return CreatedAtAction(nameof(GetQuotes), new { id = quote.Id }, responseDto);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace VoisinApp.Models;

public class Ad
{
    public string Title { get; init; } = "";

    // "À vendre", "À donner" ou "Services"
    public string Type { get; init; } = "";

    public string Price { get; init; } = "";

    // Ex. : "À 350 m · Hier"
    public string Info { get; init; } = "";

    // Nom du fichier image dans Resources/Images
    public string Image { get; init; } = "";

    public string TypeLabel => Type.ToUpperInvariant();

    public Color TypeColor => Type switch
    {
        "À vendre" => Color.FromArgb("#B5473A"),
        "À donner" => Color.FromArgb("#186B58"),
        _ => Color.FromArgb("#2F6F73")
    };
}

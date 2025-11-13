using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace artapi.Models;

public class User : IdentityUser
{
    [NotMapped]
    public UserRole Role
    {
        get => Enum.TryParse<UserRole>(RoleString, out var parsed) ? parsed : UserRole.User;
        set => RoleString = value.ToString();
    }

    // Stored in DB
    public string RoleString { get; set; } = UserRole.User.ToString();

    public List<Transaction> Transactions { get; set; } = [];
}

using System.ComponentModel.DataAnnotations;

namespace API;

public class Role
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public ICollection<Account> Accounts { get; set; } = new List<Account>();
}


using System.ComponentModel.DataAnnotations;
 namespace Api.Models.Products;
public class CreateProductRequest
{

    [Required]
    [MaxLength(100)]
    public string name {get;set;} = string.Empty;

    [MaxLength(500)]
    public string ? Description {get;set;}

    [Range(0.01,double.MaxValue)]
    public decimal Price {get;set;}
}
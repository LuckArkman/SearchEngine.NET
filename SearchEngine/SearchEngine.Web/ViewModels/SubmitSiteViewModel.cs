using System.ComponentModel.DataAnnotations;

namespace SearchEngine.Web.ViewModels;

public class SubmitSiteViewModel
{
    [Required(ErrorMessage = "A URL é obrigatória.")]
    [Url(ErrorMessage = "Por favor, insira uma URL válida.")]
    public string Url { get; set; }
}
using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Domain.Enums;

public enum MonitorType
{
    [Display(Name = "ICMP")]
    Icmp = 0,

    [Display(Name = "HTTP")]
    Http = 1,

    [Display(Name = "HTTPS")]
    Https = 2,
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KSeF.Client.Core.Models.Invoices.Query;

public class Buyer
{
    public IdentifierType IdentifierType { get; set; }
    public string Identifier { get; set; }
}

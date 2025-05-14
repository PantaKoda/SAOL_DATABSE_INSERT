using System;
using System.Collections.Generic;

namespace SAOL_DATABSE_INSERT.Models;

public partial class noun_entry
{
    public int id { get; set; }

    public string _class { get; set; } = null!;

    public virtual ICollection<noun_form> noun_forms { get; set; } = new List<noun_form>();
}

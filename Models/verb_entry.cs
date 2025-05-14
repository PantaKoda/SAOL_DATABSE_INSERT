using System;
using System.Collections.Generic;

namespace SAOL_DATABSE_INSERT.Models;

public partial class verb_entry
{
    public int id { get; set; }

    public string _class { get; set; } = null!;

    public virtual ICollection<verb_form> verb_forms { get; set; } = new List<verb_form>();
}

using System;
using System.Collections.Generic;

namespace SAOL_DATABSE_INSERT.Models;

public partial class adjective_entry
{
    public int id { get; set; }

    public string _class { get; set; } = null!;

    public virtual ICollection<adjective_form> adjective_forms { get; set; } = new List<adjective_form>();
}

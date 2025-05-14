using System;
using System.Collections.Generic;

namespace SAOL_DATABSE_INSERT.Models;

public partial class verb_form
{
    public int entry_id { get; set; }

    public string section { get; set; } = null!;

    public string form { get; set; } = null!;

    public virtual verb_entry entry { get; set; } = null!;
}

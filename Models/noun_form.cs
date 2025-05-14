using System;
using System.Collections.Generic;

namespace SAOL_DATABSE_INSERT.Models;

public partial class noun_form
{
    public int entry_id { get; set; }

    public string number { get; set; } = null!;

    public string form { get; set; } = null!;

    public virtual noun_entry entry { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace SAOL_DATABSE_INSERT.Models;

public partial class adjective_form
{
    public int entry_id { get; set; }

    public string degree { get; set; } = null!;

    public string form { get; set; } = null!;

    public virtual adjective_entry entry { get; set; } = null!;
}

using System;
using System.Collections.Generic;

namespace SAOL_DATABSE_INSERT.Models;

public partial class adverb_form
{
    public int entry_id { get; set; }

    public string form { get; set; } = null!;

    public virtual adverb_entry entry { get; set; } = null!;
}

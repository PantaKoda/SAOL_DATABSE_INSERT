using System;
using System.Collections.Generic;

namespace SAOL_DATABSE_INSERT.Models;

public partial class adverb_entry
{
    public int id { get; set; }

    public string _class { get; set; } = null!;

    public virtual ICollection<adverb_form> adverb_forms { get; set; } = new List<adverb_form>();
}

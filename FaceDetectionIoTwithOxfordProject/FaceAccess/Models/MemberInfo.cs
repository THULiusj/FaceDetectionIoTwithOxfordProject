namespace FaceAccess.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("MemberInfo")]
    public partial class MemberInfo
    {
        [Key]
        public Guid MemberId { get; set; }

        public string LastName { get; set; }

        public string FirstName { get; set; }

        public string Permission { get; set; }
    }
}

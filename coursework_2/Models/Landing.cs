using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace coursework_2.Models
{
    [Table("LandingSections")]
    public class LandingSection
    {
        public static readonly string[] Types = { "Hero", "Features", "About", "Stats", "Testimonials", "FAQ", "CTA" };
        public static readonly string[] BackgroundStyles = { "light", "muted", "primary", "dark" };

        [Key]
        public int SectionId { get; set; }

        [Required, StringLength(40)]
        public string SectionType { get; set; }

        [Required, StringLength(160)]
        public string Heading { get; set; }

        [StringLength(300)]
        public string Subheading { get; set; }

        [StringLength(2000)]
        public string Body { get; set; }

        [StringLength(300)]
        public string ImagePath { get; set; }

        [StringLength(20)]
        public string BackgroundStyle { get; set; }

        [StringLength(80)]
        public string ButtonText { get; set; }

        [StringLength(300)]
        public string ButtonUrl { get; set; }

        public int SortOrder { get; set; }

        public bool IsPublished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<LandingSectionItem> Items { get; set; }

        public LandingSection()
        {
            Items = new List<LandingSectionItem>();
            IsPublished = true;
            BackgroundStyle = "light";
        }

        public static bool SupportsItems(string sectionType)
        {
            return sectionType == "Features" || sectionType == "Stats"
                || sectionType == "Testimonials" || sectionType == "FAQ";
        }
    }

    [Table("LandingSectionItems")]
    public class LandingSectionItem
    {
        [Key]
        public int ItemId { get; set; }

        public int SectionId { get; set; }

        [Required, StringLength(160)]
        public string Title { get; set; }

        [StringLength(1200)]
        public string Text { get; set; }

        [StringLength(300)]
        public string ImagePath { get; set; }

        [StringLength(80)]
        public string LinkText { get; set; }

        [StringLength(300)]
        public string LinkUrl { get; set; }

        public int SortOrder { get; set; }

        [ForeignKey("SectionId")]
        public virtual LandingSection Section { get; set; }
    }
}

namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddLandingPage : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.LandingSectionItems",
                c => new
                    {
                        ItemId = c.Int(nullable: false, identity: true),
                        SectionId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 160),
                        Text = c.String(maxLength: 1200),
                        ImagePath = c.String(maxLength: 300),
                        LinkText = c.String(maxLength: 80),
                        LinkUrl = c.String(maxLength: 300),
                        SortOrder = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ItemId)
                .ForeignKey("dbo.LandingSections", t => t.SectionId, cascadeDelete: true)
                .Index(t => t.SectionId);
            
            CreateTable(
                "dbo.LandingSections",
                c => new
                    {
                        SectionId = c.Int(nullable: false, identity: true),
                        SectionType = c.String(nullable: false, maxLength: 40),
                        Heading = c.String(nullable: false, maxLength: 160),
                        Subheading = c.String(maxLength: 300),
                        Body = c.String(maxLength: 2000),
                        ImagePath = c.String(maxLength: 300),
                        BackgroundStyle = c.String(maxLength: 20),
                        ButtonText = c.String(maxLength: 80),
                        ButtonUrl = c.String(maxLength: 300),
                        SortOrder = c.Int(nullable: false),
                        IsPublished = c.Boolean(nullable: false),
                        CreatedAt = c.DateTime(nullable: false),
                        UpdatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SectionId);
            
            AddColumn("dbo.SystemSettings", "LandingPageEnabled", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.LandingSectionItems", "SectionId", "dbo.LandingSections");
            DropIndex("dbo.LandingSectionItems", new[] { "SectionId" });
            DropColumn("dbo.SystemSettings", "LandingPageEnabled");
            DropTable("dbo.LandingSections");
            DropTable("dbo.LandingSectionItems");
        }
    }
}

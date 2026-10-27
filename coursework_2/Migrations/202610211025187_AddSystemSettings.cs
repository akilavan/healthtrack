namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSystemSettings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SystemSettings",
                c => new
                    {
                        SettingId = c.Int(nullable: false, identity: true),
                        CompanyName = c.String(nullable: false, maxLength: 120),
                        Tagline = c.String(maxLength: 200),
                        LogoPath = c.String(maxLength: 300),
                        SupportEmail = c.String(maxLength: 150),
                        SupportPhone = c.String(maxLength: 30),
                        CancellationWindowHours = c.Int(nullable: false),
                        UpdatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SettingId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.SystemSettings");
        }
    }
}

namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRecaptchaSettings : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SystemSettings", "RecaptchaEnabled", c => c.Boolean(nullable: false));
            AddColumn("dbo.SystemSettings", "RecaptchaSiteKey", c => c.String(maxLength: 100));
            AddColumn("dbo.SystemSettings", "RecaptchaSecretKey", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SystemSettings", "RecaptchaSecretKey");
            DropColumn("dbo.SystemSettings", "RecaptchaSiteKey");
            DropColumn("dbo.SystemSettings", "RecaptchaEnabled");
        }
    }
}

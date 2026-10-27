namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSmsGateway : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SmsMessages",
                c => new
                    {
                        SmsMessageId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(),
                        Phone = c.String(maxLength: 30),
                        Message = c.String(maxLength: 500),
                        Purpose = c.String(maxLength: 30),
                        Status = c.String(maxLength: 20),
                        ProviderResponse = c.String(maxLength: 500),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.SmsMessageId);
            
            AddColumn("dbo.Users", "PhoneVerified", c => c.Boolean(nullable: false));
            Sql("UPDATE dbo.Users SET PhoneVerified = 1");
            AddColumn("dbo.Users", "PhoneVerificationCode", c => c.String(maxLength: 10));
            AddColumn("dbo.Users", "PhoneVerificationSentAt", c => c.DateTime());
            AddColumn("dbo.Users", "PhoneVerificationExpiresAt", c => c.DateTime());
            AddColumn("dbo.Users", "PhoneVerificationAttempts", c => c.Int(nullable: false));
            AddColumn("dbo.SystemSettings", "SmsEnabled", c => c.Boolean(nullable: false));
            AddColumn("dbo.SystemSettings", "SmsApiKey", c => c.String(maxLength: 200));
            AddColumn("dbo.SystemSettings", "SmsSenderId", c => c.String(maxLength: 20));
            AddColumn("dbo.SystemSettings", "SmsApiUrl", c => c.String(maxLength: 300));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SystemSettings", "SmsApiUrl");
            DropColumn("dbo.SystemSettings", "SmsSenderId");
            DropColumn("dbo.SystemSettings", "SmsApiKey");
            DropColumn("dbo.SystemSettings", "SmsEnabled");
            DropColumn("dbo.Users", "PhoneVerificationAttempts");
            DropColumn("dbo.Users", "PhoneVerificationExpiresAt");
            DropColumn("dbo.Users", "PhoneVerificationSentAt");
            DropColumn("dbo.Users", "PhoneVerificationCode");
            DropColumn("dbo.Users", "PhoneVerified");
            DropTable("dbo.SmsMessages");
        }
    }
}

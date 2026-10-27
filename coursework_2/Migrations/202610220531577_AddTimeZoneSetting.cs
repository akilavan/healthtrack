namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddTimeZoneSetting : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.SystemSettings", "TimeZoneId", c => c.String(maxLength: 100));
        }
        
        public override void Down()
        {
            DropColumn("dbo.SystemSettings", "TimeZoneId");
        }
    }
}

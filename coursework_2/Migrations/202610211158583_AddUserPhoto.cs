namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddUserPhoto : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "PhotoPath", c => c.String(maxLength: 300));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "PhotoPath");
        }
    }
}

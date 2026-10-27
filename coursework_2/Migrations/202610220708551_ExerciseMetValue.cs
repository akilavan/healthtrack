namespace coursework_2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ExerciseMetValue : DbMigration
    {
        public override void Up()
        {
            RenameColumn(table: "dbo.Exercises", name: "CaloriesPerMinute", newName: "MetValue");
            Sql("UPDATE dbo.Exercises SET MetValue = ROUND(MetValue / 1.225, 1)");
        }
        
        public override void Down()
        {
            Sql("UPDATE dbo.Exercises SET MetValue = ROUND(MetValue * 1.225, 1)");
            RenameColumn(table: "dbo.Exercises", name: "MetValue", newName: "CaloriesPerMinute");
        }
    }
}

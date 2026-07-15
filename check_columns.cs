using System;
using System.Data;
using System.Data.SqlClient;

class CheckColumns
{
    static void Main()
    {
        var connStr = "Server=(localdb)\\TradingDBInstance;Database=TradingCourseDb;Trusted_Connection=True;TrustServerCertificate=True;";
        using var conn = new SqlConnection(connStr);
        conn.Open();
        using var cmd = new SqlCommand("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID('HomepageBanners') AND name IN ('ButtonText','ButtonUrl')", conn);
        using var reader = cmd.ExecuteReader();
        var found = new System.Collections.Generic.List<string>();
        while (reader.Read()) found.Add(reader.GetString(0));
        Console.WriteLine("Found columns: " + (found.Count > 0 ? string.Join(", ", found) : "NONE"));
        if (found.Count < 2)
        {
            Console.WriteLine("Missing columns! Applying migration manually...");
            reader.Close();
            using var alter = new SqlConnection(connStr);
            alter.Open();
            using var add1 = new SqlCommand("ALTER TABLE HomepageBanners ADD ButtonText NVARCHAR(200) NULL", alter);
            add1.ExecuteNonQuery();
            using var add2 = new SqlCommand("ALTER TABLE HomepageBanners ADD ButtonUrl NVARCHAR(500) NULL", alter);
            add2.ExecuteNonQuery();
            Console.WriteLine("Columns added manually.");
        }
    }
}

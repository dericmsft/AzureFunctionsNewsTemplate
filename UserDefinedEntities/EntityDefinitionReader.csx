#load "EntityDefinition.csx"

using System.Collections;

public class EntityDefinitionReader
{
	private final string connectionString;

	public EntityDefinitionReader(string connectionString)
	{
		this.connectionString = connectionString;
	}

	public IEnumerable<EntityDefinition> LoadEntityDefinitions()
	{
		using (SqlConnection connection = new SqlConnection(connectionString))
		{
			connection.open();

            var command = new SqlCommand("SELECT regex, entityType, entityValue FROM bpst_news.userdefinedentitydefinitions", connection);

            SqlDataReader reader = command.ExecuteReader();
            var returnObject = new LinkedList<EntityDefinition>();

            if (reader.HasRows)
            {
                while (reader.Read())
				{
					returnObject.Add(new EntityDefinition()
					{
						Regex = reader["regex"].ToString(),
						EntityType = reader["entityType"].ToString(),
						EntityValue = reader["entityValue"].ToString()
					});
                }
            }

            return returnObject;
		}
	}
}

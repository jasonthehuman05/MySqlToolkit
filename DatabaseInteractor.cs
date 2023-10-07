using MySql.Data.MySqlClient;
using System.Data;
using System.Reflection;

namespace MySqlToolkit
{
    public class DatabaseInteractor
    {
        MySqlConnection conn;

        public DatabaseInteractor(string connString)
        {
            conn = new MySqlConnection(connString); //Create the conn object and open it.
            OpenConnection();
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        public void OpenConnection()
        {
            conn.Open();
        }

        /// <summary>
        /// Close the connection to the database .
        /// </summary>
        public void CloseConnection()
        {
            conn.Close();
        }

        /// <summary>
        /// Checks for null/whitespace
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private bool IsNullOrWhiteSpace(string x)
        {
            return string.IsNullOrWhiteSpace(x);
        }

        /// <summary>
        /// Converts the retrieved datatable into a List<T>
        /// </summary>
        /// <typeparam name="T">The class to build the data into</typeparam>
        /// <param name="table">The datatable containing the data to use</param>
        /// <returns></returns>
        private List<T> ToList<T>(DataTable table) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();

                foreach (var row in table.AsEnumerable())
                {
                    T obj = new T();

                    foreach (var prop in obj.GetType().GetProperties())
                    {
                        try
                        {
                            // Set the column name to be the name of the property
                            string ColumnName = prop.Name;

                            // Get a list of all of the attributes on the property
                            object[] attrs = prop.GetCustomAttributes(true);
                            foreach (object attr in attrs)
                            {
                                // Check if there is a custom property name
                                if (attr is MySqlColumn colName)
                                {
                                    // If the custom column name is specified, overwrite the property name
                                    if (!IsNullOrWhiteSpace(colName.Name))
                                        ColumnName = colName.Name;
                                }
                            }

                            PropertyInfo propertyInfo = obj.GetType().GetProperty(prop.Name);

                            // GET THE COLUMN NAME OFF THE ATTRIBUTE OR THE NAME OF THE PROPERTY
                            propertyInfo.SetValue(obj, Convert.ChangeType(row[ColumnName], propertyInfo.PropertyType), null);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a datatable from the database using the query provided
        /// </summary>
        /// <param name="Query">The query to use</param>
        /// <returns></returns>
        private DataTable GetDataTable(string Query)
        {
            try
            {
                DataTable data = new DataTable();
                using (MySqlCommand command = new MySqlCommand(Query, conn))
                {
                    data.Load(command.ExecuteReader());
                }
                return data;

            }
            catch (Exception ex)
            {
                // handle exception here
                Console.WriteLine(ex);
                throw ex;
            }
        }

        /// <summary>
        /// Get data from a database using the provided query, and return the appropriate list
        /// </summary>
        /// <returns>A List<T> containing the data</returns>
        public List<T> GetData<T>(string Query) where T : class, new()
        {
            DataTable dt = GetDataTable(Query);
            List<T> t = ToList<T>(dt);
            return t;
        }

        /// <summary>
        /// Insert data into the database from a provided object
        /// </summary>
        /// <typeparam name="T">The type of the input object</typeparam>
        /// <param name="obj">The object to use</param>
        /// <param name="tableName">The name of the table to insert the data into</param>
        public void InsertData<T>(string tableName, T obj) where T : class, new()
        {
            PropertyInfo[] properties = obj.GetType().GetProperties();

            //Get column names and asspciated values for the insert statement
            string columns = string.Join(", ", properties.Select(p =>
            {
                var colNameAttribute = p.GetCustomAttribute<MySqlColumn>();//Get attribute
                if (!colNameAttribute.IsAuto)
                {
                    string columnName = colNameAttribute != null ? colNameAttribute.Name : p.Name;//Get the name from the attrib
                    return columnName;
                }
                else
                { return null; }
            }).Where(cn => cn != null));

            string values = string.Join(", ", properties.Select(p =>
            {
                var colNameAttribute = p.GetCustomAttribute<MySqlColumn>();//Get attribute
                if (!colNameAttribute.IsAuto)
                {
                    object value = p.GetValue(obj);
                    return p.PropertyType == typeof(string) ? $"'{value}'" : value.ToString();
                }
                else { return null; }
            }).Where(vn => vn != null));

            //Build the string
            string insertQuery = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

            //Insert into db
            MySqlCommand c = conn.CreateCommand();
            c.CommandText = insertQuery;
            c.ExecuteNonQuery();
        }

        /// <summary>
        /// Run a NonQueryCommand
        /// </summary>
        /// <param name="insertQuery"></param>
        public void NonQueryCommand(string insertQuery)
        {
            //Insert into db
            MySqlCommand c = conn.CreateCommand();
            c.CommandText = insertQuery;
            c.ExecuteNonQuery();
        }
    }
}
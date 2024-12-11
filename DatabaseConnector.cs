using MySql.Data.MySqlClient;
using Mysqlx.Expr;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;


namespace База_данных_фирмы
{
    internal static class DatabaseConnector
    {
        private static string server = "localhost";
        private static string database = "factory";
        private static string user = "";
        private static string password = "";
        private static MySqlConnection connection = null;
        private static readonly object connectionLock = new object();
        private static Dictionary<string, string> ShowingTables = new Dictionary<string, string>() { 
                                                                                                   {"root",$"SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = '{database}'"},
                                                                                                   {"Начальник_производства",$"SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = '{database}'" },
                                                                                                   {"Менеджер",$"SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = '{database}' and table_name in ('наряды', 'детали', 'содержание_наряда')" },
                                                                                                   {"Клиент",$"SELECT TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = '{database}' and table_name in ('детали')" } };

        //Подключение
        public static bool Connect(string user_, string password_)
        {
            lock (connectionLock)
            {
                if (connection != null && connection.State == ConnectionState.Open) return true; //Уже подключены
                user = user_;
                password = password_;
                string connectionString = $"Server={server};Database={database};Uid={user};Pwd={password};";
                try
                {
                    connection = new MySqlConnection(connectionString);
                    connection.Open();
                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}");
                    return false;
                }
            }
        }

        //Зачем это было сделано?
        public static void Disconnect()
        {
            lock (connectionLock)
            {
                if (connection != null && connection.State == ConnectionState.Open)
                {
                    connection.Close();
                    connection.Dispose();
                    connection = null;
                }
            }
        }

        //Вывод списка пользователей
        public static DataTable GetUserListData()
        {
            lock (connectionLock)
            {
                if (connection == null || connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Не установлено соединение с базой данных.");
                try
                {
                    using (MySqlCommand command = new MySqlCommand("SELECT * FROM user_list", connection))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка получения данных: {ex.Message}");
                    return null;
                }
            }
        }

        //Вывод доступных таблиц
        public static List<string> GetAvailableTables()
        {
            lock (connectionLock)
            {
                if (connection == null || connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Не установлено соединение с базой данных.");
                string query ="";
                List<string> tables = new List<string>();
                try
                {
                    query = ShowingTables[user];
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tables.Add(reader.GetString(0));
                        }
                    }
                    return tables;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка получения списка таблиц: {ex.Message}");
                    return null;
                }
            }
        }

        //Выгрузка из таблицы
        public static DataTable GetTableData(string tableName)
        {
            lock (connectionLock)
            {
                if (connection == null || connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Не установлено соединение с базой данных.");
                try
                {
                    using (MySqlCommand command = new MySqlCommand($"SELECT * FROM {tableName}", connection))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка получения данных из таблицы {tableName}: {ex.Message}");
                    return null;
                }
            }
        }

        //Любые команды
        public static bool ExecuteSqlCommand(string sqlCommand)
        {
            lock (connectionLock)
            {
                if (connection == null || connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Не установлено соединение с базой данных.");
                try
                {
                    using (MySqlCommand command = new MySqlCommand(sqlCommand, connection))
                    {
                        command.ExecuteNonQuery();
                        MessageBox.Show($"Неужели сработало?");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка выполнения SQL-команды: {ex.Message}");
                    return false;
                }
            }
        }

        //Смена пароля
        public static bool ChangePassword(string OldPassword, string NewPassword)
        {
            if (OldPassword == password)
            {
                lock (connectionLock)
                {
                    if (connection == null || connection.State != ConnectionState.Open)
                    {
                        throw new InvalidOperationException("Не установлено соединение с базой данных.");
                    }

                    try
                    {
                        using (MySqlCommand command = new MySqlCommand(($"ALTER USER '{user}'@'{server}' IDENTIFIED BY '{NewPassword}';"), connection))
                        {
                            command.ExecuteNonQuery();
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка выполнения SQL-команды: {ex.Message}");
                        return false;
                    }
                }
            }
            else return false; 
        }


        //Удаление строк
        public static void DeleteRows(string ListDoDelete, string TableName)
        {
            if (Checker.CheckString(ListDoDelete))
                ExecuteSqlCommand($"DELETE FROM {TableName} WHERE ID IN ({ListDoDelete});");
            else
                MessageBox.Show("Перепроверьте ввод");
        }

        //Получение имён столбцов
        public static List<string> GetColumnNames(string tableName, string databaseName=null)
        {
            List<string> columnNames = new List<string>();
            databaseName = database;
            lock (connectionLock) 
            {
                using (MySqlCommand command = new MySqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @databaseName AND TABLE_NAME = @tableName",connection))
                {
                    command.Parameters.AddWithValue("@databaseName", databaseName);
                    command.Parameters.AddWithValue("@tableName", tableName);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columnNames.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return columnNames;
        }


        //Замена значений
        public static void ReplaceStringValues(string tableName, List<string> RowNames, List<string> NewValues, int ID)
        {
            string smth = $"Update {tableName} set ";
            smth += Checker.UnionLists(NewValues, RowNames);
            smth += $" where ({RowNames[0]} = '{ID}');";
            ExecuteSqlCommand(smth);
        }

        public static void AddValue(string tableName, List<string> RowNames, List<string> NewValues)
        {
            string Command = $"Insert into {tableName} ";
            Command += Checker.QueueLists(NewValues, RowNames);
            ExecuteSqlCommand(Command);
        }

        public static DataTable OperationsOfShops()
        {
            lock (connectionLock)
            {
                if (connection == null || connection.State != ConnectionState.Open)
                    throw new InvalidOperationException("Не установлено соединение с базой данных.");
                try
                {
                    using (MySqlCommand command = new MySqlCommand($" SELECT c.Название AS Название_цеха,  GROUP_CONCAT(o.Название ORDER BY o.Название SEPARATOR ', ') AS Названия_операций FROM  Цеха c JOIN  (SELECT DISTINCT ID_цеха FROM Журнал_операций) AS jc ON c.ID = jc.ID_цеха LEFT JOIN  Журнал_операций jo ON c.ID = jo.ID_цеха LEFT JOIN  Операции o ON jo.ID_операции = o.ID GROUP BY  c.ID, c.Название;  ", connection))
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        return dataTable;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка запроса: {ex.Message}");
                    return null;
                }
            }
        }
    }
}

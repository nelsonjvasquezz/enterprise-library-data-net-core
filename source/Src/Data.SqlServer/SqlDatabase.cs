// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Xml;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using Microsoft.Practices.EnterpriseLibrary.Data.Properties;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql.Configuration;

namespace Microsoft.Practices.EnterpriseLibrary.Data.Sql
{
    /// <summary>
    /// Represents a SQL Server database.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Internally uses SQL Server .NET Managed Provider from Microsoft (System.Data.SqlClient) to connect to the database.
    /// </para>
    /// </remarks>
    [ConfigurationElementType(typeof(SqlDatabaseData))]
    public partial class SqlDatabase : Database
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlDatabase"/> class with a connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public SqlDatabase(string connectionString)
            : base(connectionString, SqlClientFactory.Instance)
        {
        }

        /// <summary>
        /// Gets the parameter token used to delimit parameters for the SQL Server database.
        /// </summary>
        /// <value>
        /// <para>The '@' symbol.</para>
        /// </value>
        protected char ParameterToken => '@';

        /// <summary>
        /// Does this <see cref='Database'/> object support asynchronous execution?
        /// </summary>
        /// <value>true.</value>
        public override bool SupportsAsync
        {
            get { return true; }
        }

        /// <summary>
        /// Executes the <see cref="SqlCommand"/> and returns a new <see cref="XmlReader"/>.
        /// </summary>
        /// <remarks>
        /// When the returned reader is closed, the underlying connection will be closed
        /// (with appropriate handling of connections in the case of an ambient transaction).
        /// This is a behavior change from Enterprise Library versions prior to v5.
        /// </remarks>
        /// <param name="command">
        /// <para>The <see cref="SqlCommand"/> to execute.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="XmlReader"/> object.</para>
        /// </returns>
        public XmlReader ExecuteXmlReader(DbCommand command)
        {
            SqlCommand sqlCommand = CheckIfSqlCommand(command);

            using (var wrapper = GetOpenConnection())
            {
                PrepareCommand(command, wrapper.Connection);
                return new RefCountingXmlReader(wrapper, DoExecuteXmlReader(sqlCommand));
            }
        }

        /// <summary>
        /// Executes the <see cref="SqlCommand"/> in a transaction and returns a new <see cref="XmlReader"/>.
        /// </summary>
        /// <remarks>
        ///        Unlike other Execute... methods that take a <see cref="DbCommand"/> instance, this method
        ///        does not set the command behavior to close the connection when you close the reader.
        ///        That means you'll need to close the connection yourself, by calling the
        ///        command.Connection.Close() method.
        /// </remarks>
        /// <param name="command">
        /// <para>The <see cref="SqlCommand"/> to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="XmlReader"/> object.</para>
        /// </returns>
        public XmlReader ExecuteXmlReader(DbCommand command, DbTransaction transaction)
        {
            SqlCommand sqlCommand = CheckIfSqlCommand(command);

            PrepareCommand(sqlCommand, transaction);
            return DoExecuteXmlReader(sqlCommand);
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <see cref="SqlCommand"/> which will result in a <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="command">
        /// <para>The <see cref="SqlCommand"/> to execute.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <seealso cref="ExecuteXmlReader(DbCommand)"/>
        /// <seealso cref="EndExecuteXmlReader"/>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteXmlReader"/>,
        /// which returns the <see cref="XmlReader"/> object.</para>
        /// </returns>
        public IAsyncResult BeginExecuteXmlReader(DbCommand command, AsyncCallback callback, object state)
        {
            SqlCommand sqlCommand = CheckIfSqlCommand(command);

            DbConnection connection = GetNewOpenConnection();
            try
            {
                PrepareCommand(command, connection);
                return DoBeginExecuteXmlReader(sqlCommand, callback, state);
            }
            catch
            {
                connection.Close();
                throw;
            }
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <see cref="SqlCommand"/> inside a transaction which will result in a <see cref="XmlReader"/>.
        /// </summary>
        /// <param name="command">
        /// <para>The <see cref="SqlCommand"/> to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="IDbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <seealso cref="ExecuteXmlReader(DbCommand, DbTransaction)"/>
        /// <seealso cref="EndExecuteXmlReader"/>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteXmlReader"/>,
        /// which returns the <see cref="XmlReader"/> object.</para>
        /// </returns>
        public IAsyncResult BeginExecuteXmlReader(DbCommand command, DbTransaction transaction, AsyncCallback callback, object state)
        {
            SqlCommand sqlCommand = CheckIfSqlCommand(command);

            PrepareCommand(sqlCommand, transaction);
            return DoBeginExecuteXmlReader(sqlCommand, callback, state);
        }

        /// <summary>
        /// Finishes asynchronous execution of a Transact-SQL statement, returning the requested data as XML.
        /// </summary>
        /// <param name="asyncResult">
        /// <para>The <see cref="IAsyncResult"/> returned by a call to any overload of <see cref="BeginExecuteXmlReader(DbCommand, AsyncCallback, object)"/>.</para>
        /// </param>
        /// <seealso cref="ExecuteXmlReader(DbCommand)"/>
        /// <seealso cref="BeginExecuteXmlReader(DbCommand, AsyncCallback, object)"/>
        /// <seealso cref="BeginExecuteXmlReader(DbCommand, DbTransaction, AsyncCallback, object)"/>
        /// <returns>
        /// <para>An <see cref="XmlReader"/> object that can be used to fetch the resulting XML data.</para>
        /// </returns>
        public XmlReader EndExecuteXmlReader(IAsyncResult asyncResult)
        {
            var daabAsyncResult = (DaabAsyncResult)asyncResult;
            var command = (SqlCommand)daabAsyncResult.Command;
            try
            {
                XmlReader reader = command.EndExecuteXmlReader(daabAsyncResult.InnerAsyncResult);

                if (command.Transaction == null)
                {
                    using (var wrapper = new DatabaseConnectionWrapper(command.Connection))
                    {
                        return new RefCountingXmlReader(wrapper, reader);
                    }
                }
                return reader;
            }
            catch (Exception)
            {
                if (command.Transaction == null)
                {
                    // for a reader, the standard cleanup will not close the connection, so it needs to be closed
                    // in the catch block if necessary
                    command.Connection.Close();
                }
                throw;
            }
            finally
            {
                CleanupConnectionFromAsyncOperation(daabAsyncResult);
            }
        }

        private IAsyncResult DoBeginExecuteXmlReader(SqlCommand command, AsyncCallback callback, object state)
        {
            return WrappedAsyncOperation.BeginAsyncOperation(
                callback,
                cb => command.BeginExecuteXmlReader(cb, state),
                ar => new DaabAsyncResult(ar, command, false, false, DateTime.Now));
        }

        /// <summary>
        /// Execute the actual XML Reader call.
        /// </summary>
        private XmlReader DoExecuteXmlReader(SqlCommand sqlCommand)
        {
            XmlReader reader = sqlCommand.ExecuteXmlReader();
            return reader;
        }

        private static SqlCommand CheckIfSqlCommand(DbCommand command)
        {
            SqlCommand sqlCommand = command as SqlCommand;
            if (sqlCommand == null) throw new ArgumentException(Resources.ExceptionCommandNotSqlCommand, nameof(command));
            return sqlCommand;
        }

        /// <summary>
        /// Listens for the RowUpdate event on a data adapter to support UpdateBehavior.Continue
        /// </summary>
        private void OnSqlRowUpdated(object sender, SqlRowUpdatedEventArgs rowThatCouldNotBeWritten)
        {
            if (rowThatCouldNotBeWritten.RecordsAffected == 0)
            {
                if (rowThatCouldNotBeWritten.Errors != null)
                {
                    rowThatCouldNotBeWritten.Row.RowError = Resources.ExceptionMessageUpdateDataSetRowFailure;
                    rowThatCouldNotBeWritten.Status = UpdateStatus.SkipCurrentRow;
                }
            }
        }

        /// <summary>
        /// Does this <see cref='Database'/> object support parameter discovery?
        /// </summary>
        /// <value>true.</value>
        public override bool SupportsParemeterDiscovery
        {
            get { return true; }
        }

        /// <summary>
        /// Retrieves parameter information from the stored procedure specified in the <see cref="DbCommand"/> and populates the Parameters collection of the specified <see cref="DbCommand"/> object.
        /// </summary>
        /// <param name="discoveryCommand">The <see cref="DbCommand"/> to do the discovery.</param>
        /// <remarks>The <see cref="DbCommand"/> must be a <see cref="SqlCommand"/> instance.</remarks>
        protected override void DeriveParameters(DbCommand discoveryCommand)
        {
            SqlCommandBuilder.DeriveParameters((SqlCommand)discoveryCommand);
        }

        /// <summary>
        /// Returns the starting index for parameters in a command.
        /// </summary>
        /// <returns>The starting index for parameters in a command.</returns>
        protected override int UserParametersStartIndex()
        {
            return 1;
        }

        /// <summary>
        /// Builds a value parameter name for the current database.
        /// </summary>
        /// <param name="name">The name of the parameter.</param>
        /// <returns>A correctly formated parameter name.</returns>
        public override string BuildParameterName(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (name[0] != ParameterToken)
            {
                return name.Insert(0, new string(ParameterToken, 1));
            }
            return name;
        }

        /// <summary>
        /// Sets the RowUpdated event for the data adapter.
        /// </summary>
        /// <param name="adapter">The <see cref="DbDataAdapter"/> to set the event.</param>
        protected override void SetUpRowUpdatedEvent(DbDataAdapter adapter)
        {
            ((SqlDataAdapter)adapter).RowUpdated += OnSqlRowUpdated;
        }

        /// <summary>
        /// Determines if the number of parameters in the command matches the array of parameter values.
        /// </summary>
        /// <param name="command">The <see cref="DbCommand"/> containing the parameters.</param>
        /// <param name="values">The array of parameter values.</param>
        /// <returns><see langword="true"/> if the number of parameters and values match; otherwise, <see langword="false"/>.</returns>
        protected override bool SameNumberOfParametersAndValues(DbCommand command, object[] values)
        {
            int returnParameterCount = 1;
            int numberOfParametersToStoredProcedure = command.Parameters.Count - returnParameterCount;
            int numberOfValuesProvidedForStoredProcedure = values.Length;
            return numberOfParametersToStoredProcedure == numberOfValuesProvidedForStoredProcedure;
        }

        /// <summary>
        /// Adds a new instance of a <see cref="DbParameter"/> object to the command.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>
        /// <param name="size"><para>The maximum size of the data within the column.</para></param>
        /// <param name="direction"><para>One of the <see cref="ParameterDirection"/> values.</para></param>
        /// <param name="nullable"><para>A value indicating whether the parameter accepts <see langword="null"/> (<b>Nothing</b> in Visual Basic) values.</para></param>
        /// <param name="precision"><para>The maximum number of digits used to represent the <paramref name="value"/>.</para></param>
        /// <param name="scale"><para>The number of decimal places to which <paramref name="value"/> is resolved.</para></param>
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the <paramref name="value"/>.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>
        public virtual void AddParameter(DbCommand command, string name, SqlDbType dbType, int size, ParameterDirection direction, bool nullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));

            DbParameter parameter = CreateParameter(name, dbType, size, direction, nullable, precision, scale, sourceColumn, sourceVersion, value);
            command.Parameters.Add(parameter);
        }

        /// <summary>
        /// Adds a new instance of a <see cref="DbParameter"/> object to the command.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="SqlDbType"/> values.</para></param>
        /// <param name="direction"><para>One of the <see cref="ParameterDirection"/> values.</para></param>
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the <paramref name="value"/>.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>
        public void AddParameter(DbCommand command, string name, SqlDbType dbType, ParameterDirection direction, string sourceColumn, DataRowVersion sourceVersion, object value)
        {
            AddParameter(command, name, dbType, 0, direction, false, 0, 0, sourceColumn, sourceVersion, value);
        }

        /// <summary>
        /// Adds a new Out <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the out parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="SqlDbType"/> values.</para></param>
        /// <param name="size"><para>The maximum size of the data within the column.</para></param>
        public void AddOutParameter(DbCommand command, string name, SqlDbType dbType, int size)
        {
            AddParameter(command, name, dbType, size, ParameterDirection.Output, true, 0, 0, String.Empty, DataRowVersion.Default, DBNull.Value);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the in parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="SqlDbType"/> values.</para></param>
        /// <remarks>
        /// <para>This version of the method is used when you can have the same parameter object multiple times with different values.</para>
        /// </remarks>
        public void AddInParameter(DbCommand command, string name, SqlDbType dbType)
        {
            AddParameter(command, name, dbType, ParameterDirection.Input, String.Empty, DataRowVersion.Default, null);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="SqlDbType"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>
        public void AddInParameter(DbCommand command, string name, SqlDbType dbType, object value)
        {
            AddParameter(command, name, dbType, ParameterDirection.Input, String.Empty, DataRowVersion.Default, value);
        }

        /// <summary>
        /// Adds a new In <see cref="DbParameter"/> object to the given <paramref name="command"/>.
        /// </summary>
        /// <param name="command">The command to add the parameter.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="SqlDbType"/> values.</para></param>
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the value.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        public void AddInParameter(DbCommand command, string name, SqlDbType dbType, string sourceColumn, DataRowVersion sourceVersion)
        {
            AddParameter(command, name, dbType, 0, ParameterDirection.Input, true, 0, 0, sourceColumn, sourceVersion, null);
        }

        /// <summary>
        /// Adds a new instance of a <see cref="DbParameter"/> object.
        /// </summary>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="DbType"/> values.</para></param>
        /// <param name="size"><para>The maximum size of the data within the column.</para></param>
        /// <param name="direction"><para>One of the <see cref="ParameterDirection"/> values.</para></param>
        /// <param name="nullable"><para>A value indicating whether the parameter accepts <see langword="null"/> (<b>Nothing</b> in Visual Basic) values.</para></param>
        /// <param name="precision"><para>The maximum number of digits used to represent the <paramref name="value"/>.</para></param>
        /// <param name="scale"><para>The number of decimal places to which <paramref name="value"/> is resolved.</para></param>
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the <paramref name="value"/>.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>
        protected DbParameter CreateParameter(string name, SqlDbType dbType, int size, ParameterDirection direction, bool nullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
        {
            SqlParameter param = CreateParameter(name) as SqlParameter;
            ConfigureParameter(param, name, dbType, size, direction, nullable, precision, scale, sourceColumn, sourceVersion, value);
            return param;
        }

        /// <summary>
        /// Configures a given <see cref="DbParameter"/>.
        /// </summary>
        /// <param name="param">The <see cref="DbParameter"/> to configure.</param>
        /// <param name="name"><para>The name of the parameter.</para></param>
        /// <param name="dbType"><para>One of the <see cref="SqlDbType"/> values.</para></param>
        /// <param name="size"><para>The maximum size of the data within the column.</para></param>
        /// <param name="direction"><para>One of the <see cref="ParameterDirection"/> values.</para></param>
        /// <param name="nullable"><para>A value indicating whether the parameter accepts <see langword="null"/> (<b>Nothing</b> in Visual Basic) values.</para></param>
        /// <param name="precision"><para>The maximum number of digits used to represent the <paramref name="value"/>.</para></param>
        /// <param name="scale"><para>The number of decimal places to which <paramref name="value"/> is resolved.</para></param>
        /// <param name="sourceColumn"><para>The name of the source column mapped to the DataSet and used for loading or returning the <paramref name="value"/>.</para></param>
        /// <param name="sourceVersion"><para>One of the <see cref="DataRowVersion"/> values.</para></param>
        /// <param name="value"><para>The value of the parameter.</para></param>
        protected virtual void ConfigureParameter(SqlParameter param, string name, SqlDbType dbType, int size, ParameterDirection direction, bool nullable, byte precision, byte scale, string sourceColumn, DataRowVersion sourceVersion, object value)
        {
            param.SqlDbType = dbType;
            param.Size = size;
            param.Value = value ?? DBNull.Value;
            param.Direction = direction;
            param.IsNullable = nullable;
            param.SourceColumn = sourceColumn;
            param.SourceVersion = sourceVersion;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "The purpose of the block is to execute arbitrary SQL on behalf of the user. It is known that the users must review the use of the Database for security vulnerabilities.")]
        private static SqlCommand CreateSqlCommandByCommandType(CommandType commandType, string commandText)
        {
            return new SqlCommand(commandText)
            {
                CommandType = commandType
            };
        }

        private IAsyncResult DoBeginExecuteNonQuery(SqlCommand command, bool disposeCommand, AsyncCallback callback, object state)
        {
            bool closeConnection = command.Transaction == null;

            return WrappedAsyncOperation.BeginAsyncOperation(
                callback,
                cb => command.BeginExecuteNonQuery(cb, state),
                ar => new DaabAsyncResult(ar, command, disposeCommand, closeConnection, DateTime.Now));
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <see cref="SqlCommand"/> which will return the number of affected records.
        /// </summary>
        /// <param name="command">
        /// <para>The <see cref="SqlCommand"/> to execute.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <seealso cref="Database.ExecuteNonQuery(DbCommand)"/>
        /// <seealso cref="EndExecuteNonQuery(IAsyncResult)"/>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteNonQuery"/>,
        /// which returns the number of affected records.</para>
        /// </returns>
        public override IAsyncResult BeginExecuteNonQuery(DbCommand command, AsyncCallback callback, object state)
        {
            SqlCommand sqlCommand = CheckIfSqlCommand(command);

            DbConnection connection = this.GetNewOpenConnection();
            try
            {
                PrepareCommand(sqlCommand, connection);
                return DoBeginExecuteNonQuery(sqlCommand, false, callback, state);
            }
            catch
            {
                connection.Close();
                throw;
            }
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <see cref="DbCommand"/> inside a transaction which will return the number of affected records.
        /// </summary>
        /// <param name="command">
        /// <para>The <see cref="SqlCommand"/> to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="DbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <seealso cref="Database.ExecuteNonQuery(DbCommand)"/>
        /// <seealso cref="EndExecuteNonQuery(IAsyncResult)"/>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteNonQuery"/>,
        /// which returns the number of affected records.</para>
        /// </returns>
        public override IAsyncResult BeginExecuteNonQuery(DbCommand command, DbTransaction transaction, AsyncCallback callback, object state)
        {
            SqlCommand sqlCommand = CheckIfSqlCommand(command);

            PrepareCommand(sqlCommand, transaction);
            return DoBeginExecuteNonQuery(sqlCommand, false, callback, state);
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <paramref name="storedProcedureName"/> using the given <paramref name="parameterValues" /> which will return the number of rows affected.
        /// </summary>
        /// <param name="storedProcedureName">
        /// <para>The name of the stored procedure to execute.</para>
        /// </param>
        /// <param name="parameterValues">
        /// <para>An array of parameters to pass to the stored procedure. The parameter values must be in call order as they appear in the stored procedure.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteNonQuery"/>,
        /// which returns the number of affected records.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteNonQuery(string,object[])"/>
        /// <seealso cref="EndExecuteNonQuery(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteNonQuery(string storedProcedureName, AsyncCallback callback, object state, params object[] parameterValues)
        {
            SqlCommand sqlCommand = CheckIfSqlCommand(GetStoredProcCommand(storedProcedureName, parameterValues));

            DbConnection connection = this.GetNewOpenConnection();
            try
            {
                PrepareCommand(sqlCommand, connection);
                return DoBeginExecuteNonQuery(sqlCommand, true, callback, state);
            }
            catch
            {
                connection.Close();
                throw;
            }
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <paramref name="storedProcedureName"/> using the given <paramref name="parameterValues" /> inside a transaction which will return the number of rows affected.
        /// </summary>
        /// <param name="storedProcedureName">
        /// <para>The name of the stored procedure to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="DbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <param name="parameterValues">
        /// <para>An array of parameters to pass to the stored procedure. The parameter values must be in call order as they appear in the stored procedure.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteNonQuery"/>,
        /// which returns the number of affected records.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteNonQuery(string,object[])"/>
        /// <seealso cref="EndExecuteNonQuery(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteNonQuery(DbTransaction transaction, string storedProcedureName,
            AsyncCallback callback, object state,
            params object[] parameterValues)
        {
            SqlCommand sqlCommand = CheckIfSqlCommand(GetStoredProcCommand(storedProcedureName, parameterValues));

            PrepareCommand(sqlCommand, transaction);
            return DoBeginExecuteNonQuery(sqlCommand, true, callback, state);
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> which will return the number of rows affected.
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteNonQuery"/>,
        /// which returns the number of affected records.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteNonQuery(CommandType,string)"/>
        /// <seealso cref="EndExecuteNonQuery(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteNonQuery(CommandType commandType, string commandText, AsyncCallback callback, object state)
        {
            SqlCommand sqlCommand = CreateSqlCommandByCommandType(commandType, commandText);

            DbConnection connection = this.GetNewOpenConnection();
            try
            {
                PrepareCommand(sqlCommand, connection);
                return DoBeginExecuteNonQuery(sqlCommand, true, callback, state);
            }
            catch
            {
                connection.Close();
                throw;
            }
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> inside a transaction which will return the number of rows affected.
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="DbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteNonQuery"/>,
        /// which returns the number of affected records.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteNonQuery(CommandType,string)"/>
        /// <seealso cref="EndExecuteNonQuery(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteNonQuery(DbTransaction transaction, CommandType commandType, string commandText,
            AsyncCallback callback, object state)
        {
            SqlCommand sqlCommand = CreateSqlCommandByCommandType(commandType, commandText);

            PrepareCommand(sqlCommand, transaction);
            return DoBeginExecuteNonQuery(sqlCommand, true, callback, state);
        }

        /// <summary>
        /// Finishes asynchronous execution of a Transact-SQL statement, returning the number of affected records.
        /// </summary>
        /// <param name="asyncResult">
        /// <para>The <see cref="IAsyncResult"/> returned by a call to any overload of <see cref="BeginExecuteNonQuery(DbCommand, AsyncCallback, object)"/>.</para>
        /// </param>
        /// <seealso cref="Database.ExecuteNonQuery(DbCommand)"/>
        /// <seealso cref="BeginExecuteNonQuery(DbCommand, AsyncCallback, object)"/>
        /// <seealso cref="BeginExecuteNonQuery(DbCommand, DbTransaction, AsyncCallback, object)"/>
        /// <returns>
        /// <para>The number of affected records.</para>
        /// </returns>
        public override int EndExecuteNonQuery(IAsyncResult asyncResult)
        {
            DaabAsyncResult daabAsyncResult = (DaabAsyncResult)asyncResult;
            SqlCommand command = (SqlCommand)daabAsyncResult.Command;
            try
            {
                int affected = command.EndExecuteNonQuery(daabAsyncResult.InnerAsyncResult);

                return affected;
            }
            finally
            {
                CleanupConnectionFromAsyncOperation(daabAsyncResult);
            }
        }



        /// <summary>
        /// Initiates the asynchronous execution of a <paramref name="command"/> which will return a single value.
        /// </summary>
        /// <param name="command">
        /// <para>The <see cref="SqlCommand"/> to execute.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteScalar"/>,
        /// which returns the actual result.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteScalar(DbCommand)"/>
        /// <seealso cref="EndExecuteScalar(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteScalar(DbCommand command, AsyncCallback callback, object state)
        {
            return BeginExecuteReader(command, callback, state);
        }

        /// <summary>
        /// Initiates the asynchronous execution of a <paramref name="command"/> inside a transaction which will return a single value.
        /// </summary>
        /// <param name="command">
        /// <para>The <see cref="SqlCommand"/> to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="DbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteScalar"/>,
        /// which returns the actual result.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteScalar(DbCommand, DbTransaction)"/>
        /// <seealso cref="EndExecuteScalar(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteScalar(DbCommand command, DbTransaction transaction, AsyncCallback callback, object state)
        {
            return BeginExecuteReader(command, transaction, callback, state);
        }

        /// <summary>
        /// Initiates the asynchronous execution of <paramref name="storedProcedureName"/> using the given <paramref name="parameterValues" /> which will return a single value.
        /// </summary>
        /// <param name="storedProcedureName">
        /// <para>The name of the stored procedure to execute.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <param name="parameterValues">
        /// <para>An array of parameters to pass to the stored procedure. The parameter values must be in call order as they appear in the stored procedure.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteScalar"/>,
        /// which returns the actual result.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteScalar(string, object[])"/>
        /// <seealso cref="EndExecuteScalar(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteScalar(string storedProcedureName, AsyncCallback callback, object state, params object[] parameterValues)
        {
            return BeginExecuteReader(storedProcedureName, callback, state, parameterValues);
        }

        /// <summary>
        /// Initiates the asynchronous execution of <paramref name="storedProcedureName"/> using the given <paramref name="parameterValues" /> inside a transaction which will return a single value.
        /// </summary>
        /// <param name="transaction">
        /// <para>The <see cref="DbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="storedProcedureName">
        /// <para>The name of the stored procedure to execute.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <param name="parameterValues">
        /// <para>An array of parameters to pass to the stored procedure. The parameter values must be in call order as they appear in the stored procedure.</para>
        /// </param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteScalar"/>,
        /// which returns the actual result.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteScalar(DbTransaction, string, object[])"/>
        /// <seealso cref="EndExecuteScalar(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteScalar(DbTransaction transaction, string storedProcedureName, AsyncCallback callback, object state, params object[] parameterValues)
        {
            return BeginExecuteReader(transaction, storedProcedureName, callback, state, parameterValues);
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> which will return a single value.
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteScalar"/>,
        /// which returns the actual result.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteScalar(CommandType, string)"/>
        /// <seealso cref="EndExecuteScalar(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteScalar(CommandType commandType, string commandText, AsyncCallback callback, object state)
        {
            return BeginExecuteReader(commandType, commandText, callback, state);
        }

        /// <summary>
        /// Initiates the asynchronous execution of the <paramref name="commandText"/> interpreted as specified by the <paramref name="commandType" /> inside an transaction which will return a single value.
        /// </summary>
        /// <param name="commandType">
        /// <para>One of the <see cref="CommandType"/> values.</para>
        /// </param>
        /// <param name="commandText">
        /// <para>The command text to execute.</para>
        /// </param>
        /// <param name="transaction">
        /// <para>The <see cref="DbTransaction"/> to execute the command within.</para>
        /// </param>
        /// <param name="callback">The async callback to execute when the result of the operation is available. Pass <langword>null</langword>
        /// if you don't want to use a callback.</param>
        /// <param name="state">Additional state object to pass to the callback.</param>
        /// <returns>
        /// <para>An <see cref="IAsyncResult"/> that can be used to poll or wait for results, or both;
        /// this value is also needed when invoking <see cref="EndExecuteScalar"/>,
        /// which returns the actual result.</para>
        /// </returns>
        /// <seealso cref="Database.ExecuteScalar(DbTransaction, CommandType, string)"/>
        /// <seealso cref="EndExecuteScalar(IAsyncResult)"/>
        public override IAsyncResult BeginExecuteScalar(DbTransaction transaction, CommandType commandType, string commandText,
            AsyncCallback callback, object state)
        {
            return BeginExecuteReader(transaction, commandType, commandText, callback, state);
        }

        /// <summary>
        /// Finishes asynchronous execution of a Transact-SQL statement, returning the first column of the first row in the result set returned by the query. Extra columns or rows are ignored.
        /// </summary>
        /// <param name="asyncResult">
        /// <para>The <see cref="IAsyncResult"/> returned by a call to any overload of BeginExecuteScalar.</para>
        /// </param>
        /// <seealso cref="Database.ExecuteScalar(DbCommand)"/>
        /// <seealso cref="BeginExecuteScalar(DbCommand,AsyncCallback,object)"/>
        /// <seealso cref="BeginExecuteScalar(DbCommand,DbTransaction,AsyncCallback,object)"/>
        /// <returns>
        /// <para>The value of the first column of the first row in the result set returned by the query.
        /// If the result didn't have any columns or rows <see langword="null"/> (<b>Nothing</b> in Visual Basic).</para>
        /// </returns>
        public override object EndExecuteScalar(IAsyncResult asyncResult)
        {
            using (IDataReader reader = EndExecuteReader(asyncResult))
            {
                if (!reader.Read() || reader.FieldCount == 0)
                {
                    return null;
                }
                return reader.GetValue(0);
            }
        }

        private static void CleanupConnectionFromAsyncOperation(DaabAsyncResult daabAsyncResult)
        {
            if (daabAsyncResult.DisposeCommand)
            {
                if (daabAsyncResult.Command != null)
                {
                    daabAsyncResult.Command.Dispose();
                }
            }
            if (daabAsyncResult.CloseConnection)
            {
                if (daabAsyncResult.Connection != null)
                {
                    daabAsyncResult.Connection.Close();
                }
            }
        }
    }
}

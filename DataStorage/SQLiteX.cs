using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MatterHackers.MatterControl.DataStorage
{
	public class SQLiteX : ISQLite
	{
		public void Close()
		{
			throw new NotImplementedException();
		}

		public int CreateTable(Type ty)
		{
			throw new NotImplementedException();
		}

		public int Delete(object obj)
		{
			throw new NotImplementedException();
		}

		public int DropTable(Type ty)
		{
			throw new NotImplementedException();
		}

		public T ExecuteScalar<T>(string query, params object[] args)
		{
			throw new NotImplementedException();
		}

		public int Insert(object obj)
		{
			throw new NotImplementedException();
		}

		public int InsertAll(IEnumerable objects)
		{
			throw new NotImplementedException();
		}

		public List<T> Query<T>(string query, params object[] args) where T : new()
		{
			throw new NotImplementedException();
		}

		public void RunInTransaction(Action action)
		{
			throw new NotImplementedException();
		}

		public ITableQuery<T> Table<T>() where T : new()
		{
			throw new NotImplementedException();
		}

		public int Update(object obj)
		{
			throw new NotImplementedException();
		}
	}
}

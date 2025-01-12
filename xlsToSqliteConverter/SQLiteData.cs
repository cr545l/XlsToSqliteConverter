﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lofle.XlsToSqliteConverter
{
	public class SQLiteData
	{
		private string _sheetName = null;
		private string[] _columns = null;
		private string[] _types = null;
		private string[] _comments = null;
		private object[,] _datas = null;

		public string[] Columns => _columns;
		public string SheetName => _sheetName;

		public object[,] Datas
		{
			get { return _datas; }
			private set
			{
				_datas = value;
				_columns = GetArray<string>( _datas, Constant._COLUMNS_INDEX, i => _datas[Constant._COLUMNS_INDEX, i + 1]?.ToString() );
				_types = GetArray<string>( _datas, Constant._TYPES_INDEX, i => _datas[Constant._TYPES_INDEX, i + 1]?.ToString() );
				_comments = GetArray<string>( _datas, Constant._COMMENT_INDEX, i => _datas[Constant._COMMENT_INDEX, i + 1]?.ToString() );

				if( null != _columns && null != _types && null != _comments )
				{
					for( int i = 0; i < _columns.Length; i++ )
					{
						if( null == _columns[i] )
						{
							Array.Resize( ref _columns, i );
							Array.Resize( ref _types, i );
							Array.Resize( ref _comments, i );

							break;
						}
					}
				}
			}
		}

		public static SQLiteData Create( string sheetName, dynamic datas )
		{
			if( null != sheetName && null != datas )
			{
				return new SQLiteData( sheetName, datas );
			}
			return null;
		}

		private SQLiteData( string sheetName, dynamic datas )
		{
			_sheetName = sheetName;
			Datas = datas;
		}

		static private T[] GetArray<T>( object[,] source, int index, Func<int, T> cast )
		{
			if( null != source )
			{
				// UsedRange.Value2로 가져온 배열의 인덱스가 1부터 시작
				T[] result = new T[source.GetLength( 1 )];

				for( int i = 0; i < result.Length; i++ )
				{
					result[i] = cast( i );
				}

				return result;
			}
			return null;
		}

		//static private T[] GetArray<T>( object[,] source, int index )
		//{
		//	// UsedRange.Value2로 가져온 배열의 인덱스가 1부터 시작
		//	T[] result = new T[source.GetLength( 1 )];
		//
		//	for( int i = 0; i < result.Length; i++ )
		//	{
		//		result[i] = (T)source[index, i + 1];
		//	}
		//
		//	return result;
		//}

		public string GetColumns()
		{
			return AppendToString( ( s, i ) =>
			{
				s.Append( null != _columns[i] ? _columns[i] : "null" );
			} );
		}

		public string GetColumnAndTypes()
		{
			return AppendToString( ( s, i ) =>
			{
				if( null != _columns[i] && null != _types[i] )
				{
					s.Append( _columns[i] );
					s.Append( " " );
					s.Append( _types[i] );
				}
			} );
		}

		private string AppendToString( Action<StringBuilder, int> callback )
		{
			StringBuilder result = new StringBuilder();

			if( null != _columns )
			{
				for( int i = 0; i < _columns.Length; i++ )
				{
					if( 0 != i )
					{
						result.Append( ", " );
					}

					callback( result, i );
				}
			}

			return result.ToString();
		}

		public string GetCode( string fileName )
		{
			StringBuilder result = new StringBuilder();

			if( null != _columns && null != _types && null != _comments )
			{
				result.Append( "namespace " );
				result.Append( fileName );
				result.Append( "\n{\n" );

				result.Append( "\tpublic partial class " );
				result.Append( _sheetName );
				result.Append( "\n\t{\n" );

				for( int i = 0; i < _types.Length; i++ )
				{
					if( null != _comments[i] && string.Empty != _comments[i] )
					{
						result.Append( "\t\t/// <summary>\n\t\t/// " );
						result.Append( _comments[i] );
						result.Append( "\n\t\t/// </summary>\n" );
					}
					if( null != _types[i] && isPrimaryKey( _types[i] ) )
					{
						result.Append( "\t\t[SQLite4Unity3d.PrimaryKey, SQLite4Unity3d.AutoIncrement]\n" );
					}
					result.Append( "\t\tpublic " );
					result.Append( ConvertCShapeType( _types[i] ) );
					result.Append( " " );
					result.Append( _columns[i] );
					result.Append( " { get; set; }\n\n" );
				}

				result.Append( "\t\tpublic override string ToString ()" );
				result.Append( "\n\t\t{\n" );
				result.Append( "\t\t\treturn string.Format( \"[" );
				result.Append( _sheetName );
				result.Append( ": " );

				for( int i = 0; i < _columns.Length; i++ )
				{
					result.Append( $"{_columns[i]}={{{i}}}" );
					if( i < _columns.Length - 1 )
					{
						result.Append( "," );
					}
				}
				result.Append( "]\", " );

				for( int i = 0; i < _columns.Length; i++ )
				{
					result.Append( _columns[i] );
					if( i < _columns.Length - 1 )
					{
						result.Append( ", " );
					}
				}

				result.Append( ");\n" );
				result.Append( "\t\t}\n" );

				result.Append( "\t}\n" );
				result.Append( "}\n" );
			}

			return result.ToString();
		}

		static public bool isPrimaryKey( string type )
		{
			return 0 == String.Compare( type.ToLower(), Constant._INTEGER_PRIMARY_KEY );
		}

		static public string ConvertCShapeType( string sqliteType )
		{
			if( null == sqliteType || !Constant._PARSE_DATA.ContainsKey( sqliteType.ToLower() ) )
			{
				return Constant._PARSE_DATA["string"];
			}
			else
			{
				return Constant._PARSE_DATA[sqliteType.ToLower()];
			}
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Collections;
using Aerospike.Client;

namespace AerospikeHelper.Query
{
	public class Qualifier : IDictionary<String, Object> 
	{
		private const String FIELD = "field";
		private const String VALUE2 = "value2";
		private const String VALUE1 = "value1";
		private const String OPERATION = "operation";
		private const String COLLECTION_OPERATION = "collection-operation";
		protected Dictionary<String, Object> internalMap;
		public enum FilterOperation {
			EQ, GT, GTEQ, LT, LTEQ, NOTEQ, BETWEEN, START_WITH, ENDS_WITH, 
			LIST_CONTAINS, MAP_KEYS_CONTAINS, MAP_VALUES_CONTAINS, 
			LIST_BETWEEN, MAP_KEYS_BETWEEN, MAP_VALUES_BETWEEN
		}

		public Qualifier() {
			internalMap = new Dictionary<String, Object>();
		}
		public Qualifier(String field, FilterOperation operation, Value value1) : this() {

			internalMap[FIELD] = field;
			internalMap[OPERATION] = operation;
			internalMap[VALUE1] = value1;
		}
		public Qualifier(String field, FilterOperation operation, Value value1, Value value2) : this(field, operation, value1) {
			internalMap[VALUE2] = value2;
		}



		public FilterOperation getOperation(){
			return (FilterOperation) internalMap[OPERATION];
		}
		public String Field {
			get {
				return (String)internalMap [FIELD];
			}
		}
		public Value Value1{
			get {
				return (Value)internalMap [VALUE1];
			}
		}
		public Value Value2{
			get {
				return (Value)internalMap [VALUE2];
			}
		}

		public Filter AsFilter(){
			FilterOperation op = getOperation();
			switch (op) {
			case FilterOperation.EQ:
				return BinEquality ();
			case FilterOperation.BETWEEN:
				return BinRange ();
			case FilterOperation.LIST_CONTAINS:
				return collectionContains(IndexCollectionType.LIST);
			case FilterOperation.MAP_KEYS_CONTAINS:
				return collectionContains(IndexCollectionType.MAPKEYS);
			case FilterOperation.MAP_VALUES_CONTAINS:
				return collectionContains(IndexCollectionType.MAPVALUES);
			case FilterOperation.LIST_BETWEEN:
				return collectionRange(IndexCollectionType.LIST);
			case FilterOperation.MAP_KEYS_BETWEEN:
				return collectionRange(IndexCollectionType.MAPKEYS);
			case FilterOperation.MAP_VALUES_BETWEEN:
				return collectionRange(IndexCollectionType.MAPKEYS);
			default:
				return null;
			}
		}

		private Filter BinRange(){
			return Filter.Range(Field, Value1.ToLong(), Value2.ToLong());
		}

		private Filter BinEquality(){
			int valType = Value1.Type;
			switch (valType){
			case ParticleType.INTEGER:
				return Filter.Equal(Field, Value1.ToLong());
			case ParticleType.STRING:
				return Filter.Equal(Field, Value1.ToString());
			}
			return null;
		}

		private Filter collectionContains(IndexCollectionType collectionType){
			Value val = Value1;
			int valType = val.Type;
			switch (valType){
			case ParticleType.INTEGER:
				return Filter.Contains(Field, collectionType, val.ToLong());
			case ParticleType.STRING:
				return Filter.Contains(Field, collectionType, val.ToString());
			}
			return null;
		}
		private Filter collectionRange(IndexCollectionType collectionType){
			return Filter.Range(Field, collectionType, Value1.ToLong(), Value2.ToLong());
		}

		public String luaFilterString(){
			String value1 = luaValueString(Value1);
			FilterOperation op = getOperation();
			switch (op) {
			case FilterOperation.EQ:
				return String.Format("%s == %s", luaFieldString(Field),  value1);
			case FilterOperation.LIST_CONTAINS:
				return String.Format("containsValue(%s, %s)", luaFieldString(Field),  value1);
			case FilterOperation.MAP_KEYS_CONTAINS:
				return String.Format("containsKey(%s, %s)", luaFieldString(Field),  value1);
			case FilterOperation.MAP_VALUES_CONTAINS:
				return String.Format("containsValue(%s, %s)", luaFieldString(Field),  value1);
			case FilterOperation.NOTEQ:
				return String.Format("%s ~= %s", luaFieldString(Field), value1);
			case FilterOperation.GT:
				return String.Format("%s > %s", luaFieldString(Field), value1);
			case FilterOperation.GTEQ:
				return String.Format("%s >= %s", luaFieldString(Field), value1);
			case FilterOperation.LT:
				return String.Format("%s < %s", luaFieldString(Field), value1);
			case FilterOperation.LTEQ:
				return String.Format("%s <= %s", luaFieldString(Field), value1);
			case FilterOperation.BETWEEN:
				String value2 = luaValueString(Value2);
				String fieldString = luaFieldString(Field); 
				return String.Format("%s >= %s and %s <= %s  ", fieldString, value1, luaFieldString(Field), value2);
			case FilterOperation.LIST_BETWEEN:
				value2 = luaValueString(Value2);
				return String.Format("rangeValue(%s, %s, %s)", luaFieldString(Field),  value1, value2);
			case FilterOperation.MAP_KEYS_BETWEEN:
				value2 = luaValueString(Value2);
				return String.Format("rangeKey(%s, %s, %s)", luaFieldString(Field),  value1, value2);
			case FilterOperation.MAP_VALUES_BETWEEN:
				value2 = luaValueString(Value2);
				return String.Format("rangeValue(%s, %s, %s)", luaFieldString(Field),  value1, value2);
			case FilterOperation.START_WITH:
				return String.Format("string.sub(%s,1,string.len(%s))==%s", luaFieldString(Field), value1, value1);			
			case FilterOperation.ENDS_WITH:
				return String.Format("%s=='' or string.sub(%s,-string.len(%s))==%s", 
					value1,
					luaFieldString(Field),
					value1,
					value1);			
			}
			return "";
		}

		protected String luaFieldString(String field){
			return String.Format("rec['%s']", field);
		}

		protected String luaValueString(Value value){
			String res = null;
			int type = value.Type;
			switch (type) {
			//		case ParticleType.LIST:
			//			res = value.toString();
			//			break;
			//		case ParticleType.MAP:
			//			res = value.toString();
			//			break;
			//		case ParticleType.DOUBLE:
			//			res = value.toString();
			//			break;
			case ParticleType.STRING:
				res = String.Format("'%s'", value.ToString());
				break;
			default:
				res = value.ToString();
				break;
			}
			return res;
		}
		#region IDictionary Members
		public bool IsReadOnly { get { return false; } }
		public bool ContainsKey(string key)
		{
			return internalMap.ContainsKey(key);
		}
		public bool IsFixedSize { get { return false; } }

		public bool Remove(string key)
		{
			return internalMap.Remove (key);
		}
		public bool Remove(object key)
		{
			return internalMap.Remove ((String)key);
		}
		public void Clear() { internalMap.Clear(); }

		public void Add(string key, object value) 
		{
			internalMap.Add (key, value);
		}

		public void Add(object key, object value) 
		{
			internalMap.Add ((String)key, value);
		}

		public ICollection<string> Keys
		{
			get
			{
				return internalMap.Keys;
			}
		}
		public ICollection<object> Values
		{
			get
			{
				return internalMap.Values;
			}
		}
		public object this[object key]
		{
			get
			{   
				return internalMap [(String)key];
			}

			set
			{
				internalMap [(String)key] = value;
			}
		}
		public object this[string key]
		{
			get
			{   
				return internalMap [key];
			}

			set
			{
				internalMap [key] = value;
			}
		}
		private Boolean TryGetIndexOfKey(Object key, out Object index)
		{
			return internalMap.TryGetValue((string)key, out index);
		}
		public IEnumerator GetEnumerator()
		{
			return internalMap.GetEnumerator();
		}

		public bool  TryGetValue(string key, out object value){
			return internalMap.TryGetValue (key, out value);
		}

		public int Count {
			get {
				return internalMap.Count;
			}
		}

		#endregion

	}
}

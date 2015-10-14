package com.aerospike.helper.query;

import java.io.IOException;

import org.junit.Assert;
import org.junit.Test;

import com.aerospike.client.Value;
import com.aerospike.client.query.Filter;
import com.aerospike.client.query.IndexType;
import com.aerospike.client.query.KeyRecord;
import com.aerospike.client.query.Statement;
import com.aerospike.client.task.IndexTask;

public class SelectorTest extends HelperTest{



	public SelectorTest() {
		super();
	}

	@Test
	public void selectOneWitKey() throws IOException {
		Statement stmt = new Statement();
		stmt.setNamespace(QueryEngineTests.NAMESPACE);
		stmt.setSetName(QueryEngineTests.SET_NAME);
		KeyQualifier kq = new KeyQualifier(Value.get("selector-test:3"));
		KeyRecordIterator it = queryEngine.select(stmt, kq);
		int count = 0;
		while (it.hasNext()){
			KeyRecord rec = it.next();
			count++;
			//			System.out.println(rec);
		}
		it.close();
		//		System.out.println(count);
		Assert.assertEquals(1, count);
	}

	@Test
	public void selectAll() throws IOException {
		KeyRecordIterator it = queryEngine.select(QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, null);
		try {
			while (it.hasNext()){
				KeyRecord rec = it.next();
			}
		} finally {
			it.close();
		}
	}

	@Test
	public void selectOnIndex() throws IOException {
		IndexTask task = this.client.createIndex(null, QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, "age_index", "age", IndexType.NUMERIC);
		task.waitTillComplete(50);
		Filter filter = Filter.range("age", 28, 29);
		KeyRecordIterator it = queryEngine.select(QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, filter);
		try{
			while (it.hasNext()){
				KeyRecord rec = it.next();
				int age = rec.record.getInt("age");
				Assert.assertTrue(age >= 28 && age <= 29);
			}
		} finally {
			it.close();
		}
	}
	@Test
	public void selectStartsWith() throws IOException {
		Qualifier qual1 = new Qualifier("color", Qualifier.FilterOperation.ENDS_WITH, Value.get("e"));
		KeyRecordIterator it = queryEngine.select(QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, null, qual1);
		try{
			while (it.hasNext()){
				KeyRecord rec = it.next();
				Assert.assertTrue(rec.record.getString("color").endsWith("e"));

			}
		} finally {
			it.close();
		}
	}
	@Test
	public void selectEndsWith() throws IOException {
		Qualifier qual1 = new Qualifier("color", Qualifier.FilterOperation.EQ, Value.get("blue"));
		Qualifier qual2 = new Qualifier("name", Qualifier.FilterOperation.START_WITH, Value.get("na"));
		KeyRecordIterator it = queryEngine.select(QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, null, qual1, qual2);
		try{
			while (it.hasNext()){
				KeyRecord rec = it.next();
				Assert.assertEquals("blue", rec.record.getString("color"));
				Assert.assertTrue(rec.record.getString("name").startsWith("na"));
			}
		} finally {
			it.close();
		}
	}
	@Test
	public void selectOnIndexWithQualifiers() throws IOException {
		IndexTask task = this.client.createIndex(null, QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, "age_index_selector", "age", IndexType.NUMERIC);
		task.waitTillComplete(50);
		Filter filter = Filter.range("age", 25, 29);
		Qualifier qual1 = new Qualifier("color", Qualifier.FilterOperation.EQ, Value.get("blue"));
		KeyRecordIterator it = queryEngine.select(QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, filter, qual1);
		try{
			while (it.hasNext()){
				KeyRecord rec = it.next();
				Assert.assertEquals("blue", rec.record.getString("color"));
				int age = rec.record.getInt("age");
				Assert.assertTrue(age >= 25 && age <= 29);
			}
		} finally {
			it.close();
		}
	}
	@Test
	public void selectWithQualifiersOnly() throws IOException {
		IndexTask task = this.client.createIndex(null, QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, "age_index", "age", IndexType.NUMERIC);
		task.waitTillComplete(50);
		queryEngine.refreshCluster();
		Qualifier qual1 = new Qualifier("color", Qualifier.FilterOperation.EQ, Value.get("green"));
		Qualifier qual2 = new Qualifier("age", Qualifier.FilterOperation.BETWEEN, Value.get(28), Value.get(29));
		KeyRecordIterator it = queryEngine.select(QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, null, qual1, qual2);
		try{
			while (it.hasNext()){
				KeyRecord rec = it.next();
				Assert.assertEquals("green", rec.record.getString("color"));
				int age = rec.record.getInt("age");
				Assert.assertTrue(age >= 28 && age <= 29);
			}
		} finally {
			it.close();
		}
	}
	@Test
	public void selectWithGeneration() throws IOException {
		queryEngine.refreshCluster();
		Qualifier qual1 = new GenerationQualifier(Qualifier.FilterOperation.GTEQ, Value.get(1));
		KeyRecordIterator it = queryEngine.select(QueryEngineTests.NAMESPACE, QueryEngineTests.SET_NAME, null, qual1);
		try {
			while (it.hasNext()){
				KeyRecord rec = it.next();
				Assert.assertTrue(rec.record.generation >= 1);
			}
		} finally {
			it.close();
		}
	}

}

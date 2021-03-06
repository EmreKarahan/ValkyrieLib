﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Valkyrie.Data.Mongo.Entity;
using Valkyrie.Data.Mongo.Helper;
using Valkyrie.Data.Mongo.UnitOfWork;

namespace Valkyrie.Data.Mongo.Impl
{

    public class BaseRepository<T, TKey> : IRepository<T, TKey>
        where T : IEntity<TKey>
    {
        public BaseRepository(IUnitOfWork unitOfWork)
        {
            Collection = unitOfWork.Database.GetCollection<T>(GetCollectionName());
        }

        public IMongoCollection<T> Collection { get; }

        public virtual IQueryable<T> AsQueryable()
        {
            return Collection.AsQueryable();
        }

        #region Sync Methods
        public virtual void Add(IEnumerable<T> entities)
        {
            Collection.InsertMany(entities);
        }
        public virtual T Add(T entity)
        {
            Collection.InsertOne(entity);
            return entity;
        }
        public virtual long Count()
        {
            return Collection.CountDocuments(x => true);
        }
        public virtual void Delete(Expression<Func<T, bool>> predicate)
        {
            Collection.DeleteMany<T>(predicate);
        }
        public virtual void Delete(T entity)
        {
            var filter = Builders<T>.Filter.Eq(s => s.Id, entity.Id);
            Collection.DeleteOne(filter);
        }
        public virtual void Delete(TKey id)
        {
            var filter = Builders<T>.Filter.Eq(s => s.Id, id);
            Collection.DeleteOne(filter);
        }
        public virtual void DeleteAll()
        {
            Collection.DeleteMany(x => true);
        }
        public virtual void Drop()
        {
            Collection.Database.DropCollection(Collection.CollectionNamespace.CollectionName);
        }
        public virtual bool Exists()
        {
            return Collection.AsQueryable().Any();
        }
        public virtual bool Exists(Expression<Func<T, bool>> predicate)
        {
            return Collection.AsQueryable().Any(predicate);
        }
        public virtual T GetById(TKey id)
        {
            var filter = Builders<T>.Filter.Eq(s => s.Id, id);
            return Collection.Find(filter).SingleOrDefault();
        }
        public virtual void Update(IEnumerable<T> entities)
        {
            Parallel.ForEach(entities, entity =>
            {
                Update(entity);
            });
        }
        public virtual T Update(T entity)
        {
            var filter = Builders<T>.Filter.Eq(s => s.Id, entity.Id);
            Collection.ReplaceOne(filter, entity);
            return entity;
        }
        #endregion

        #region Async Methods
        public async Task<T> GetByIdAsync(TKey id)
        {
            var filter = Builders<T>.Filter.Eq(s => s.Id, id);
            var cursor = await Collection.FindAsync(filter);
            return await cursor.SingleOrDefaultAsync();
        }
        public async Task<T> AddAsync(T entity)
        {
            await Collection.InsertOneAsync(entity);
            return entity;
        }
        public async Task AddAsync(IEnumerable<T> entities)
        {
            await Collection.InsertManyAsync(entities);
        }
        public async Task<T> UpdateAsync(T entity)
        {
            var filter = Builders<T>.Filter.Eq(s => s.Id, entity.Id);
            await Collection.ReplaceOneAsync(filter, entity);
            return entity;
        }
        public async Task UpdateAsync(IEnumerable<T> entities)
        {
            var tasks = entities.Select(async item =>
            {
                var response = await UpdateAsync(item);
            });
            await Task.WhenAll(tasks);
        }
        public async Task DeleteAsync(TKey id)
        {
            var filter = Builders<T>.Filter.Eq(s => s.Id, id);
            await Collection.DeleteOneAsync(filter);
        }
        public async Task DeleteAsync(T entity)
        {
            var filter = Builders<T>.Filter.Eq(s => s.Id, entity.Id);
            await Collection.DeleteOneAsync(filter);
        }
        public async Task DeleteAsync(Expression<Func<T, bool>> predicate)
        {
            await Collection.DeleteManyAsync<T>(predicate);
        }
        public async Task DeleteAllAsync()
        {
            await Collection.DeleteManyAsync(x => true);
        }
        public async Task DropAsync()
        {
            await Collection.Database.DropCollectionAsync(Collection.CollectionNamespace.CollectionName);
        }
        public async Task<long> CountAsync()
        {
            return await Collection.CountDocumentsAsync(x => true);
        }
        public async Task<bool> ExistsAsync()
        {
            return await IAsyncCursorSourceExtensions.AnyAsync(Collection.AsQueryable());
        }
        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await Collection.AsQueryable().AnyAsync(predicate);
        }
        #endregion

        #region Private Methods
        private static string GetCollectionName()
        {
            var collectionName = typeof(T).GetTypeInfo().BaseType == typeof(object)
                ? GetCollectioNameFromInterface()
                : GetCollectionNameFromType(typeof(T));

            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException("Collection name cannot be empty for this entity");
            return collectionName;
        }
        private static string GetCollectioNameFromInterface()
        {
            // Check to see if the object (inherited from Entity) has a CollectionName attribute
            var att = typeof(T).GetTypeInfo().GetCustomAttribute<CollectionName>();
            var collectionname = att != null ? att.Name : typeof(T).Name;

            return collectionname;
        }
        private static string GetCollectionNameFromType(Type entitytype)
        {
            string collectionname;

            // Check to see if the object (inherited from Entity) has a CollectionName attribute
            var att = entitytype.GetTypeInfo().GetCustomAttribute<CollectionName>();
            if (att != null)
            {
                // It does! Return the value specified by the CollectionName attribute
                collectionname = att.Name;
            }
            else
            {
                if (typeof(BaseEntity).GetTypeInfo().IsAssignableFrom(entitytype))
                    while (entitytype.GetTypeInfo().BaseType != typeof(BaseEntity))
                        entitytype = entitytype.GetTypeInfo().BaseType;
                collectionname = entitytype.Name;
            }

            return collectionname;
        }
        #endregion

        #region IQueryable<T>
        public virtual IEnumerator<T> GetEnumerator()
        {
            return Collection.AsQueryable().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Collection.AsQueryable().GetEnumerator();
        }
        public virtual Type ElementType => Collection.AsQueryable().ElementType;
        public virtual Expression Expression => Collection.AsQueryable().Expression;
        public virtual IQueryProvider Provider => Collection.AsQueryable().Provider;
        #endregion
    }

    public class BaseRepository<T> : BaseRepository<T, string>, IRepository<T>
        where T : IEntity<string>
    {
        public BaseRepository(IUnitOfWork unitOfWork) : base(unitOfWork)
        {
        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cloud.Core.Testing;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Xunit;

namespace Cloud.Core.Storage.AzureCosmos.Tests.IntegrationTests
{
    [IsIntegration]
    public class CosmosStorageIntegrationTest
    {
        private readonly ITableStorage _cosmosClient;

        public CosmosStorageIntegrationTest()
        {
            var readConfig = new ConfigurationBuilder().AddJsonFile("appSettings.json").Build();

            var config = new Config.ServicePrincipleConfig
            {
                InstanceName = readConfig.GetValue<string>("InstanceName"),
                TenantId = readConfig.GetValue<string>("TenantId"),
                SubscriptionId = readConfig.GetValue<string>("SubscriptionId"),
                DatabaseName = "Test",
                AppId = readConfig.GetValue<string>("AppId"),
                AppSecret = readConfig.GetValue<string>("AppSecret"),
                CreateDatabaseIfNotExists = true
            };

            _cosmosClient = new CosmosStorage(config);
        }

        /// <summary>Verify entities can be created and deleted as expected.</summary>
        [Fact]
        public async Task Test_CosmosStorage_DeleteEntitites()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                // Arrange
                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var nameKey = Guid.NewGuid().ToString();
                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                // Act/Assert - setup and delete entities confirming created and deleted.
                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);
                var entityOneExists = await _cosmosClient.Exists(containerName, key);
                var entityTwoExists = await _cosmosClient.Exists(containerName, key2);

                entityOneExists.Should().Be(true);
                entityTwoExists.Should().Be(true);

                var listOfKeys = new List<string>() { key, key2 };

                await _cosmosClient.DeleteEntities(containerName, listOfKeys);

                entityOneExists = await _cosmosClient.Exists(containerName, key);
                entityTwoExists = await _cosmosClient.Exists(containerName, key2);

                entityOneExists.Should().Be(false);
                entityTwoExists.Should().Be(false);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        /// <summary>Ensure upserting entities works as expecting.</summary>
        [Fact]
        public async Task Test_CosmosStorage_UpsertEntities()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                // Arrange
                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();
                var nameKey = Guid.NewGuid().ToString();
                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };
                var entities = new List<SampleEntity> { entity, entity2 };

                // Act
                await _cosmosClient.UpsertEntities(containerName, entities);
                var entityOneExists = await _cosmosClient.Exists(containerName, key);
                var entityTwoExists = await _cosmosClient.Exists(containerName, key2);

                // Assert
                entityOneExists.Should().Be(true);
                entityTwoExists.Should().Be(true);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListTableNames()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                var containerNames = await _cosmosClient.ListTableNames();

                containerNames.Should().Contain(containerName);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_CountItems()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                //Create an entity to count
                var key = Guid.NewGuid().ToString();
                var entity = new SampleEntity() { Key = key, Name = "Name", OtherField = "other1", OtherField2 = 1 };
                await _cosmosClient.UpsertEntity(containerName, entity);

                var actionHit = false;

                await _cosmosClient.CountItems(containerName, (count) => { actionHit = true; });

                Thread.Sleep(1000);

                actionHit.Should().BeTrue();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_UpsertSingle()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var key = Guid.NewGuid().ToString();

            try
            {
                await _cosmosClient.CreateTable(containerName);

                var entity = new SampleEntity() { Key = key, Name = "Name", OtherField = "other1", OtherField2 = 1 };

                await _cosmosClient.UpsertEntity(containerName, entity);

                //Act - ensure there's an object to check for.                       
                var exists = await _cosmosClient.Exists(containerName, key);

                // Assert
                exists.Should().Be(true);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesEnumerableNoToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntities<SampleEntity>(containerName, $"SELECT * FROM c WHERE c.Name = '{nameKey}'");

                // Assert
                entities.Count().Should().Be(2);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesRetrieveFirstValid()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntities<SampleEntity>(containerName, $"SELECT * FROM c WHERE c.Name = '{nameKey}'");

                var firstEntity = entities.First();

                // Assert
                firstEntity.Should().NotBeNull();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesRetrieveFirstFailWhenIsNoEntity()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                var nameKey = Guid.NewGuid().ToString();

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntities<SampleEntity>(containerName, $"SELECT * FROM c WHERE c.Name = '{nameKey}'");

                Assert.Throws<InvalidOperationException>(() => entities.First());
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesEnumerableWithToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                var token = new CancellationTokenSource();

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntities<SampleEntity>(containerName, $"SELECT * FROM c WHERE c.Name = '{nameKey}'", token);

                // Assert
                entities.Count().Should().Be(2);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesEnumerableWithTokenAndNoQuery()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                var token = new CancellationTokenSource();

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntities<SampleEntity>(containerName, token);

                // Assert
                entities.Count().Should().BeGreaterThan(0);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesEnumerableWithCollumnsToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                var columns = new List<string>() { "Name", "Key" };

                var token = new CancellationTokenSource();

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntities<SampleEntity>(containerName, columns, $"WHERE c.Name = '{nameKey}'", token).ToList();

                //Confirm entity is retrieved with only the selected fields
                entities.Count().Should().Be(2);

                entities[0].Key.Should().NotBeNullOrEmpty();
                entities[0].Name.Should().NotBeNullOrEmpty();
                entities[0].OtherField.Should().BeNull();

                entities[1].Key.Should().NotBeNullOrEmpty();
                entities[1].Name.Should().NotBeNullOrEmpty();
                entities[1].OtherField.Should().BeNull();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesEnumerableWithCollumnsAndNoToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                var columns = new List<string>() { "Name", "Key" };

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntities<SampleEntity>(containerName, columns, $"WHERE c.Name = '{nameKey}'").ToList();

                //Confirm entity is retrieved with only the selected fields
                entities.Count().Should().Be(2);

                entities[0].Key.Should().NotBeNullOrEmpty();
                entities[0].Name.Should().NotBeNullOrEmpty();
                entities[0].OtherField.Should().BeNull();

                entities[1].Key.Should().NotBeNullOrEmpty();
                entities[1].Name.Should().NotBeNullOrEmpty();
                entities[1].OtherField.Should().BeNull();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesObservableNoToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                int count = 0;
                int loops = 0;

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntitiesObservable<SampleEntity>(containerName, $"SELECT * FROM c WHERE c.Name = '{nameKey}'").Subscribe(e =>
                {
                    count++;
                });

                // Wait for subscription.
                do
                {
                    Thread.Sleep(500);
                    loops++;
                } while (loops < 5 || count == 0);

                // Assert
                count.Should().Be(2);
                entities.Should().NotBeNull();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesObservableWithToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                int count = 0;
                int loops = 0;

                var token = new CancellationTokenSource();

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntitiesObservable<SampleEntity>(containerName, $"SELECT * FROM c WHERE c.Name = '{nameKey}'", token).Subscribe(e =>
                {
                    count++;
                });

                // Wait for subscription.
                do
                {
                    Thread.Sleep(500);
                    loops++;
                } while (loops < 5 || count == 0);

                // Assert
                count.Should().Be(2);
                entities.Should().NotBeNull();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesObservableWithTokenAndNoQuery()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                int count = 0;
                int loops = 0;

                var token = new CancellationTokenSource();

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntitiesObservable<SampleEntity>(containerName, token).Subscribe(e =>
                {
                    count++;
                });

                // Wait for subscription.
                do
                {
                    Thread.Sleep(500);
                    loops++;
                } while (loops < 5 || count == 0);

                // Assert
                count.Should().BeGreaterThan(0);
                entities.Should().NotBeNull();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesObservableWithCollumnsTokenAndQuery()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                int count = 0;
                int loops = 0;

                var token = new CancellationTokenSource();

                var columns = new List<string>() { "Name", "Key" };
                var retrievedEntities = new List<SampleEntity>();

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntitiesObservable<SampleEntity>(containerName, columns, $"WHERE c.Name = '{nameKey}'", token).Subscribe(e =>
                {
                    retrievedEntities.Add(e);
                    count++;
                });

                // Wait for subscription.
                do
                {
                    Thread.Sleep(500);
                    loops++;
                } while (loops < 5 || count == 0);

                //Confirm entity is retrieved with only the selected fields
                count.Should().Be(2);

                retrievedEntities[0].Key.Should().NotBeNullOrEmpty();
                retrievedEntities[0].Name.Should().NotBeNullOrEmpty();
                retrievedEntities[0].OtherField.Should().BeNull();

                retrievedEntities[1].Key.Should().NotBeNullOrEmpty();
                retrievedEntities[1].Name.Should().NotBeNullOrEmpty();
                retrievedEntities[1].OtherField.Should().BeNull();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_ListEntitiesObservableWithCollumnsNoTokenAndNoQuery()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                int count = 0;
                int loops = 0;

                var token = new CancellationTokenSource();

                var columns = new List<string>() { "Name", "Key" };
                var retrievedEntities = new List<SampleEntity>();

                //Act - ensure there's an object to check for.           
                var entities = _cosmosClient.ListEntitiesObservable<SampleEntity>(containerName, columns, token).Subscribe(e =>
                {
                    retrievedEntities.Add(e);
                    count++;
                });

                // Wait for subscription.
                do
                {
                    Thread.Sleep(500);
                    loops++;
                } while (loops < 5 || count == 0);

                //Confirm entites are retrieved with only the selected fields
                count.Should().Be(2);

                retrievedEntities[0].Key.Should().NotBeNullOrEmpty();
                retrievedEntities[0].Name.Should().NotBeNullOrEmpty();
                retrievedEntities[0].OtherField.Should().BeNull();

                retrievedEntities[1].Key.Should().NotBeNullOrEmpty();
                retrievedEntities[1].Name.Should().NotBeNullOrEmpty();
                retrievedEntities[1].OtherField.Should().BeNull();
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_CountEntitiesWithQueryAndToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                var token = new CancellationTokenSource();

                //Act - ensure there's an object to check for.           
                var entities = await _cosmosClient.CountItemsQuery(containerName, "SELECT * FROM c", token);

                // Assert
                entities.Should().BeGreaterThan(1);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_CountEntitiesWithQueryNoToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                //Act - ensure there's an object to check for.           
                var entities = await _cosmosClient.CountItemsQuery(containerName, "SELECT * FROM c");

                // Assert
                entities.Should().BeGreaterThan(1);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_CountEntitiesWithNoQueryAndToken()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                var token = new CancellationTokenSource();

                //Act - ensure there's an object to check for.           
                var entities = await _cosmosClient.CountItems(containerName, token);

                // Assert
                entities.Should().BeGreaterThan(1);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_CountEntitiesWithKey()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                var key = Guid.NewGuid().ToString();
                var key2 = Guid.NewGuid().ToString();

                var nameKey = Guid.NewGuid().ToString();

                var entity = new SampleEntity() { Key = key, Name = nameKey, OtherField = "other1" };
                var entity2 = new SampleEntity() { Key = key2, Name = nameKey, OtherField = "other1" };

                await _cosmosClient.UpsertEntity(containerName, entity);
                await _cosmosClient.UpsertEntity(containerName, entity2);

                //Act - ensure there's an object to check for.           
                var entities = await _cosmosClient.CountItems(containerName, nameKey);

                // Assert
                entities.Should().Be(2);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_GetEntity()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                // Arrange - create test entity.
                var key = Guid.NewGuid().ToString();
                var entity = new SampleEntity() { Key = key, Name = "Name", OtherField = "other1" };
                await _cosmosClient.UpsertEntity(containerName, entity);

                // Act - ensure there's an object to check for.
                var result = await _cosmosClient.GetEntity<SampleEntity>(containerName, key);
                await _cosmosClient.DeleteEntity(containerName, key);

                // Assert
                result.Key.Should().Be(entity.Key);
                result.Name.Should().Be(entity.Name);
                result.OtherField.Should().Be(entity.OtherField);

            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_GetEntityThatDoesntExistReturnsNull()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                // Arrange - create test entity.
                var key = Guid.NewGuid().ToString();

                // Act - ensure there's an object to check for.
                var result = await _cosmosClient.GetEntity<SampleEntity>(containerName, key);

                // Assert
                result.Should().Be(null);

            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_DeleteSingle()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                // Arrange - create test entity.
                var key = Guid.NewGuid().ToString();
                var entity = new SampleEntity() { Key = key, Name = "name1", OtherField = "other1" };
                await _cosmosClient.UpsertEntity(containerName, entity);

                // Act - ensure there's an object to check for.
                var exists = await _cosmosClient.Exists(containerName, key);
                await _cosmosClient.DeleteEntity(containerName, key);
                var existsAfterDeletion = await _cosmosClient.Exists(containerName, key);

                // Assert
                exists.Should().Be(true);
                existsAfterDeletion.Should().Be(false);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_CheckExists()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                // Arrange - create test entity.
                var key = Guid.NewGuid().ToString();
                var entity = new SampleEntity() { Key = key, Name = "name1", OtherField = "other1" };
                await _cosmosClient.UpsertEntity(containerName, entity);

                // Act - ensure there's an object to check for.
                var exists = await _cosmosClient.Exists(containerName, key);
                await _cosmosClient.DeleteEntity(containerName, key);
                var existsAfterDeletion = await _cosmosClient.Exists(containerName, key);

                // Assert
                exists.Should().Be(true);
                existsAfterDeletion.Should().Be(false);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_PartitionTests()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container with name as partition key
                await _cosmosClient.CreateTable(containerName + "/Name");

                //add an object            
                var key = "name1/" + Guid.NewGuid().ToString();
                var entity = new SampleEntity() { Key = key, Name = "name1", OtherField = "other1" };
                await _cosmosClient.UpsertEntity(containerName, entity);

                //add a second object
                var secondKey = "name2/" + Guid.NewGuid().ToString();
                var secondEntity = new SampleEntity() { Key = secondKey, Name = "name2", OtherField = "other1" };
                await _cosmosClient.UpsertEntity(containerName, secondEntity);

                //check first object exists
                var exists = await _cosmosClient.Exists(containerName, key);
                exists.Should().Be(true);

                //retrieve the second object
                var retrievedEntity = await _cosmosClient.GetEntity<SampleEntity>(containerName, secondKey);
                var expectedId = secondKey.Replace("name2/", "");

                retrievedEntity.Key.Should().Be(expectedId);
                retrievedEntity.Id.Should().Be(expectedId);
                retrievedEntity.Name.Should().Be("name2");
                retrievedEntity.OtherField.Should().Be("other1");
                retrievedEntity.OtherField2.Should().BeNull();

                //delete first object
                await _cosmosClient.DeleteEntity(containerName, key);

                //confirm deletion
                var existsAfterDeletion = await _cosmosClient.Exists(containerName, key);
                existsAfterDeletion.Should().Be(false);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_CreateContainerAndDeleteContainerWithNoPartition()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                //???
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_CreateContainerAndDeleteContainerWithPartition()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName + "/ParitionKey");
                //???
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        [Fact]
        public async Task Test_CosmosStorage_AddToCreatedTableNoPartition()
        {
            var containerName = Guid.NewGuid().ToString().Replace("-", string.Empty);

            try
            {
                //create container
                await _cosmosClient.CreateTable(containerName);

                //add an object            
                var key = Guid.NewGuid().ToString();
                var entity = new SampleEntity() { Key = key, Name = "Name", OtherField = "other1" };
                await _cosmosClient.UpsertEntity(containerName, entity);

                //check first object exists
                var exists = await _cosmosClient.Exists(containerName, key);
                exists.Should().Be(true);
            }
            finally
            {
                //Cleanup table
                await _cosmosClient.DeleteTable(containerName);
            }
        }

        private class SampleEntity : ITableItem
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public string OtherField { get; set; }
            public int? OtherField2 { get; set; }
            public bool OtherField3 { get; set; }

            [JsonProperty("id")]
            public string Id => Key;
        }
    }
}
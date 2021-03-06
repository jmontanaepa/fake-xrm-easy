﻿using System;
using System.Linq;

using Xunit;
using FakeItEasy;
using FakeXrmEasy;
using Microsoft.Xrm.Sdk.Query;

using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

using FakeXrmEasy.Tests.PluginsForTesting;
using Crm;
using System.Reflection;

namespace FakeXrmEasy.Tests
{
    public class FakeContextTestPlugins
    {
        [Fact]
        public void When_a_plugin_with_target_is_executed_the_inherent_plugin_was_also_executed_without_exceptions()
        {
            var fakedContext = new XrmFakedContext();
            
            var guid1 = Guid.NewGuid();
            var target = new Entity("contact") { Id = guid1 };

            //Execute our plugin against the selected target
            var fakedPlugin = fakedContext.ExecutePluginWithTarget<RetrieveServicesPlugin>(target);

            //Assert that the plugin was executed      
            A.CallTo(() => fakedPlugin.Execute(A<IServiceProvider>._))
                .MustHaveHappened();            
            
        }

        [Fact]
        public void When_the_account_number_plugin_is_executed_it_adds_a_random_number_to_an_account_entity()
        {
            var fakedContext = new XrmFakedContext();

            var guid1 = Guid.NewGuid();
            var target = new Entity("account") { Id = guid1 };
            
            //Execute our plugin against a target that doesn't contains the accountnumber attribute
            var fakedPlugin = fakedContext.ExecutePluginWithTarget<AccountNumberPlugin>(target);

            //Assert that the target contains a new attribute      
            Assert.True(target.Attributes.ContainsKey("accountnumber"));

        }

        [Fact]
        public void When_the_account_number_plugin_is_executed_for_an_account_that_already_has_a_number_exception_is_thrown()
        {
            var fakedContext = new XrmFakedContext();

            var guid1 = Guid.NewGuid();
            var target = new Entity("account") { Id = guid1 };
            target["accountnumber"] = 69;

            //Execute our plugin against a target thatcontains the accountnumber attribute will throw exception
            Assert.Throws<InvalidPluginExecutionException>(() => fakedContext.ExecutePluginWithTarget<AccountNumberPlugin>(target));

        }

        [Fact]
        public void When_the_followup_plugin_is_executed_for_an_account_it_should_create_a_new_task()
        {
            var fakedContext = new XrmFakedContext();
            fakedContext.ProxyTypesAssembly = Assembly.GetExecutingAssembly(); //Needed to be able to return early bound entities

            var guid1 = Guid.NewGuid();
            var target = new Entity("account") { Id = guid1 };

            fakedContext.ExecutePluginWithTarget<FollowupPlugin>(target);

            //The plugin creates a followup activity, check that that one exists
            var tasks = (from t in fakedContext.CreateQuery<Task>()
                             select t).ToList();

            Assert.True(tasks.Count == 1);
            Assert.True(tasks[0]["subject"].Equals("Send e-mail to the new customer."));
        }

        [Fact]
        public void When_the_followup_plugin_is_executed_for_an_account_after_create_it_should_create_a_new_task_with_a_regardingid()
        {
            var fakedContext = new XrmFakedContext();
            fakedContext.ProxyTypesAssembly = Assembly.GetExecutingAssembly(); //Needed to be able to return early bound entities

            var guid1 = Guid.NewGuid();
            var target = new Entity("account") { Id = guid1 };

            ParameterCollection inputParameters = new ParameterCollection();
            inputParameters.Add("Target", target);

            ParameterCollection outputParameters = new ParameterCollection();
            outputParameters.Add("id", guid1);

            fakedContext.ExecutePluginWith<FollowupPlugin>(inputParameters,outputParameters,null,null);

            //The plugin creates a followup activity, check that that one exists
            var tasks = (from t in fakedContext.CreateQuery<Task>()
                         select t).ToList();

            Assert.True(tasks.Count == 1);
            Assert.True(tasks[0].Subject.Equals("Send e-mail to the new customer."));
            Assert.True(tasks[0].RegardingObjectId != null && tasks[0].RegardingObjectId.Id.Equals(guid1));
        }
    }
}

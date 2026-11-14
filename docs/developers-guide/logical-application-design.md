# Logical Application Design

**Sections**

1.  [Preliminaries](preliminaries.md)
2.  [Implementing Data Types](implementing-data-types.md)
3.  [Representing Data with Tables and Views](representing-data.md)
4.  [Enforcing Business Rules Declaratively](enforcing-business-rules.md)
5.  [Modeling Process Logic](modeling-process-logic.md)
6.  [Enforcing Stimulus-Response Rules](enforcing-stimulus-response-rules.md)
7.  [Enforcing Security](enforcing-security.md)
8.  [Working With Libraries](working-with-libraries.md)
9.  [Database Design Techniques](database-design-techniques.md)

## Summary

This part approaches the problem of declarative application development from the logical perspective. It discusses modeling the core structures and processes of an application as a database schema. It also introduces the running example, a hypothetical shipping application which will be used as a vehicle for the concepts and techniques discussed throughout this part, as well as the next two parts of the manual. This part builds on the concepts introduced in Part I of this manual, and will refer to them extensively without explanation.

This part, together with Parts III and IV, will discuss declarative development from three main perspectives, the logical model, the presentation layer, and the physical realization. Not coincidentally, these three layers correspond roughly with the three main layers of a Database Management System, the conceptual level, the external level, and the internal, or physical level. In addition, the breakdown corresponds with the order in which a Dataphor application will typically be built, beginning with the logical model, building the presentation layer, and finally, mapping the model to a physical implementation.

The discussion begins with designing the application and implementing the resulting design in terms of the logical model. Part III then considers how the logical model is presented to the user, and Part IV discusses how the logical model is mapped onto the physical layer to achieve persistence.

This part is organized as follows:

**Preliminaries**  
This chapter discusses the Dataphor development environment, as well as some basic concepts and techniques for developing Dataphor applications. It also introduces the Shipping application, and a simplified database design to begin the implementation.

**Implementing Data Types**  
This chapter begins the implementation of the shipping application by selecting and building the set of data types that will be used to represent the data.

**Representing Data With Tables and Views**  
This chapter uses the types constructed in Implementing Data Types to create the tables and views necessary to implement the database design for the running example introduced in Preliminaries.

**Enforcing Business Rules Declaratively**  
This chapter uses the various integrity enforcement features of the D4 language to ensure that the data represented within the application conforms to the requirements of the application.

**Modeling Process Logic**  
This chapter uses the imperative features of the D4 language to model the process logic required to carry out the operations of the shipping application.

**Enforcing Stimulus-Response Rules**  
This chapter discusses the use of event handlers to enforce business rules that require some action to be taken on the part of the application, or *stimulus-response* rules.

**Enforcing Security**  
This chapter utilizes the security features of the Dataphor product to enforce the administrative requirements of the shipping application.

**Working With Libraries**  
This chapter discusses how libraries and dependency tracking are used to permit large scale organization of the application schema, as well as facilitate deployment and maintenance of the application.

**Database Design Techniques**  
This chapter discusses database design techniques and methodologies that can be used to model applications, and introduces a simple notation for expressing these designs visually.

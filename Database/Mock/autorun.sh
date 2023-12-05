docker build -t proserv-mock-db .

docker run -d --name proserv-mock-db-container -p 5432:5432 proserv-mock-db
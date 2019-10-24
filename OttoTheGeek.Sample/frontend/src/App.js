import React from 'react'
import GraphiQL from 'graphiql'
import './App.css'
import 'graphiql/graphiql.css'

function graphQLFetcher (graphQLParams) {
  return window.fetch(window.location.origin + '/graphql', {
    method: 'post',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(graphQLParams)
  }).then(response => response.json())
}

function App () {
  return (
    <GraphiQL fetcher={graphQLFetcher} />
  )
}

export default App
